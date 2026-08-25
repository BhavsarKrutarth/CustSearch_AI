using System.Globalization;
using System.Text.Json;
using CustSearch.Application.Authentication;
using CustSearch.Application.PreferencesVoice;
using CustSearch.Application.ShopperCustomers;
using CustSearch.Application.TenantOperations;
using CustSearch.Domain.Entities;
using CustSearch.Domain.Enums;
using CustSearch.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CustSearch.Infrastructure.PreferencesVoice;

/// <summary>Phase 10 preference/voice implementation. Tenant/store identity is server-derived; voice text resolves only to existing store/tenant categories and aliases.</summary>
public sealed class PreferencesVoiceService(
    CustSearchDbContext db,
    ICurrentUserContext currentUser,
    ITenantOperationsService tenantOperations,
    TimeProvider timeProvider):IPreferencesVoiceService
{
    public async Task<CustomerPreferencesView> GetCustomerPreferencesAsync(long customerId,CancellationToken cancellationToken=default)
    {
        var customer=await RequireVisibleCustomerAsync(customerId,cancellationToken).ConfigureAwait(false);
        return await MapCustomerPreferencesAsync(customer,cancellationToken).ConfigureAwait(false);
    }

    public async Task<CustomerPreferencesView> AddCustomerTagAsync(long customerId,AddCustomerPreferenceCommand command,TenantAuditContext audit,CancellationToken cancellationToken=default)
    {
        ArgumentNullException.ThrowIfNull(command);ValidateAudit(audit);var tenantId=RequireTenantId();
        var customer=await RequireVisibleCustomerAsync(customerId,cancellationToken).ConfigureAwait(false);await RequireCustomerAtStoreAsync(customerId,command.StoreId,cancellationToken).ConfigureAwait(false);
        await ValidateReferenceAsync(command.PreferenceType,command.ReferenceId,command.Value,command.StoreId,cancellationToken).ConfigureAwait(false);
        var now=UtcNow();
        CustomerPreferenceSignal signal;
        try{signal=CustomerPreferenceSignal.Create(tenantId,command.StoreId,customerId,command.PreferenceType,command.ReferenceId,command.Value,command.SignalScore,PreferenceSignalSource.ManualStaff,command.Confidence,audit.ActorUserId,command.Reason,now);}
        catch(ArgumentException ex){throw new TenantBusinessRuleException(ex.Message);}
        db.CustomerPreferenceSignals.Add(signal);RecordAudit(tenantId,command.StoreId,audit,"CustomerPreferenceManualTagAdded","CustomerPreferenceSignal",0,null,new{customerId,command.PreferenceType,command.ReferenceId,command.Value,command.SignalScore,command.Confidence,command.Reason},now);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);return await MapCustomerPreferencesAsync(customer,cancellationToken).ConfigureAwait(false);
    }

    public async Task<CustomerPreferencesView> RecalculateCustomerAsync(long customerId,TenantAuditContext audit,CancellationToken cancellationToken=default)
    {
        ValidateAudit(audit);var tenantId=RequireTenantId();var customer=await RequireVisibleCustomerAsync(customerId,cancellationToken).ConfigureAwait(false);var now=UtcNow();
        await SyncPurchaseSignalsAsync(customerId,audit.ActorUserId,cancellationToken).ConfigureAwait(false);
        var weights=await EnsureActiveWeightVersionAsync(audit.ActorUserId,cancellationToken).ConfigureAwait(false);
        var signals=await db.CustomerPreferenceSignals.Where(x=>x.TenantId==tenantId&&x.CustomerId==customerId&&x.IsActive).ToListAsync(cancellationToken).ConfigureAwait(false);
        var existing=await db.CustomerPreferenceScores.Where(x=>x.TenantId==tenantId&&x.CustomerId==customerId).ToListAsync(cancellationToken).ConfigureAwait(false);db.CustomerPreferenceScores.RemoveRange(existing);
        foreach(var group in signals.GroupBy(x=>new{x.PreferenceType,x.ReferenceId,Value=x.Value??string.Empty}))
        {
            decimal numerator=0,denominator=0;
            foreach(var signal in group){var weight=WeightFor(weights,signal.Source);var baseScore=signal.SignalScore??100m;var confidence=(signal.Confidence??100m)/100m;numerator+=baseScore*confidence*weight;denominator+=weight;}
            var score=denominator==0?0:decimal.Round(Math.Min(100m,numerator/denominator),2,MidpointRounding.AwayFromZero);
            db.CustomerPreferenceScores.Add(CustomerPreferenceScore.Create(tenantId,customerId,group.Key.PreferenceType,group.Key.ReferenceId,string.IsNullOrEmpty(group.Key.Value)?null:group.Key.Value,score,weights.Id,now));
        }
        RecordAudit(tenantId,null,audit,"CustomerPreferencesRecalculated","Customer",customerId,new{PreviousScoreCount=existing.Count},new{SignalCount=signals.Count,WeightVersion=weights.VersionCode},now);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);return await MapCustomerPreferencesAsync(customer,cancellationToken).ConfigureAwait(false);
    }

    public async Task<HouseholdPreferencesView> GetHouseholdPreferencesAsync(long householdId,CancellationToken cancellationToken=default)
    {
        var tenantId=RequireTenantId();var household=await db.Households.AsNoTracking().SingleOrDefaultAsync(x=>x.TenantId==tenantId&&x.Id==householdId&&x.IsActive,cancellationToken).ConfigureAwait(false)??throw new TenantResourceNotFoundException("Household");
        var verifiedIds=await db.HouseholdMembers.AsNoTracking().Where(x=>x.TenantId==tenantId&&x.HouseholdId==householdId&&x.IsActive&&x.IsVerified).Select(x=>x.CustomerId).ToArrayAsync(cancellationToken).ConfigureAwait(false);
        if(!IsTenantWide())
        {
            verifiedIds=await db.CustomerStoreAssignments.AsNoTracking().Where(x=>x.TenantId==tenantId&&verifiedIds.Contains(x.CustomerId)&&currentUser.StoreIds.Contains(x.StoreId)).Select(x=>x.CustomerId).Distinct().ToArrayAsync(cancellationToken).ConfigureAwait(false);
            if(verifiedIds.Length==0)throw new TenantResourceNotFoundException("Household");
        }
        var customers=await db.Customers.AsNoTracking().Where(x=>x.TenantId==tenantId&&verifiedIds.Contains(x.Id)).ToDictionaryAsync(x=>x.Id,cancellationToken).ConfigureAwait(false);
        var scores=await db.CustomerPreferenceScores.AsNoTracking().Where(x=>x.TenantId==tenantId&&verifiedIds.Contains(x.CustomerId)).ToListAsync(cancellationToken).ConfigureAwait(false);
        var memberViews=verifiedIds.Where(customers.ContainsKey).Select(id=>new HouseholdMemberPreferenceView(id,DisplayName(customers[id]),scores.Where(x=>x.CustomerId==id).OrderByDescending(x=>x.Score).Select(MapScore).ToArray())).ToArray();
        var aggregate=scores.GroupBy(x=>new{x.PreferenceType,x.ReferenceId,Value=x.Value??string.Empty}).Select(g=>new PreferenceScoreView(0,g.Key.PreferenceType,g.Key.ReferenceId,string.IsNullOrEmpty(g.Key.Value)?null:g.Key.Value,decimal.Round(g.Average(x=>x.Score),2),g.OrderByDescending(x=>x.CalculatedUtc).First().WeightVersionId,g.Max(x=>x.CalculatedUtc))).OrderByDescending(x=>x.Score).ToArray();
        var tags=await db.HouseholdPreferenceTags.AsNoTracking().Where(x=>x.TenantId==tenantId&&x.HouseholdId==householdId&&x.IsActive).OrderByDescending(x=>x.CreatedUtc).Select(x=>new HouseholdTagView(x.Id,x.PreferenceType,x.ReferenceId,x.Value,x.Source,x.Reason,x.CreatedUtc)).ToArrayAsync(cancellationToken).ConfigureAwait(false);
        return new(household.Id,household.Name,memberViews,aggregate,tags);
    }

    public async Task<HouseholdPreferencesView> AddHouseholdTagAsync(long householdId,AddHouseholdPreferenceTagCommand command,TenantAuditContext audit,CancellationToken cancellationToken=default)
    {
        ArgumentNullException.ThrowIfNull(command);ValidateAudit(audit);var tenantId=RequireTenantId();_ = await GetHouseholdPreferencesAsync(householdId,cancellationToken).ConfigureAwait(false);
        await ValidateReferenceAsync(command.PreferenceType,command.ReferenceId,command.Value,null,cancellationToken).ConfigureAwait(false);var now=UtcNow();
        HouseholdPreferenceTag tag;try{tag=HouseholdPreferenceTag.Create(tenantId,householdId,command.PreferenceType,command.ReferenceId,command.Value,command.Source,audit.ActorUserId,command.Reason,now);}catch(ArgumentException ex){throw new TenantBusinessRuleException(ex.Message);}
        db.HouseholdPreferenceTags.Add(tag);RecordAudit(tenantId,null,audit,"HouseholdPreferenceTagAdded","HouseholdPreferenceTag",0,null,new{householdId,command.PreferenceType,command.ReferenceId,command.Value,command.Source,command.Reason},now);await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return await GetHouseholdPreferencesAsync(householdId,cancellationToken).ConfigureAwait(false);
    }

    public async Task<PreferenceWeightView> GetActiveWeightVersionAsync(CancellationToken cancellationToken=default)=>MapWeight(await EnsureActiveWeightVersionAsync(currentUser.UserId,cancellationToken).ConfigureAwait(false));

    public async Task<PreferenceWeightView> SaveWeightVersionAsync(SavePreferenceWeightCommand command,TenantAuditContext audit,CancellationToken cancellationToken=default)
    {
        ArgumentNullException.ThrowIfNull(command);ValidateAudit(audit);var tenantId=RequireTenantId();var now=UtcNow();var existing=await db.PreferenceWeightVersions.Where(x=>x.TenantId==tenantId&&x.IsActive).ToListAsync(cancellationToken).ConfigureAwait(false);foreach(var item in existing)item.Deactivate();
        PreferenceWeightVersion version;try{version=PreferenceWeightVersion.Create(tenantId,command.VersionCode,command.ManualStaffWeight,command.PurchaseWeight,command.CategoryInteractionWeight,command.VoiceConfirmedWeight,audit.ActorUserId,now);}catch(ArgumentException ex){throw new TenantBusinessRuleException(ex.Message);}
        db.PreferenceWeightVersions.Add(version);RecordAudit(tenantId,null,audit,"PreferenceWeightVersionChanged","PreferenceWeightVersion",0,existing.Select(MapWeight).ToArray(),command,now);await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);return MapWeight(version);
    }

    public async Task<VoiceSettingView> GetVoiceSettingAsync(long storeId,CancellationToken cancellationToken=default)
    {
        var baseSetting=await tenantOperations.GetVoiceSettingAsync(storeId,cancellationToken).ConfigureAwait(false);var tenantId=RequireTenantId();var runtime=await db.StoreVoiceCommandRuntimeSettings.AsNoTracking().SingleOrDefaultAsync(x=>x.TenantId==tenantId&&x.StoreId==storeId,cancellationToken).ConfigureAwait(false);
        return new(storeId,baseSetting.TriggerKeyword,baseSetting.ResponseMode.ToString(),baseSetting.IsEnabled,baseSetting.RequireConfirmationForAmbiguousCategory,baseSetting.Aliases,runtime?.LanguageCode??"en-IN",runtime?.RequireConfirmation??true,runtime?.ListeningTimeoutSeconds??30,runtime?.MinimumRecognitionConfidence??70m);
    }

    public async Task<VoiceSettingView> SaveVoiceSettingAsync(long storeId,SaveVoiceRuntimeSettingCommand command,TenantAuditContext audit,CancellationToken cancellationToken=default)
    {
        ArgumentNullException.ThrowIfNull(command);ValidateAudit(audit);var tenantId=RequireTenantId();if(!Enum.TryParse<VoiceResponseMode>(command.ResponseMode,true,out var mode)||!Enum.IsDefined(mode))throw new TenantBusinessRuleException("Voice response mode is invalid.");
        var before=await GetVoiceSettingAsync(storeId,cancellationToken).ConfigureAwait(false);await tenantOperations.SaveVoiceSettingAsync(storeId,new(command.TriggerKeyword,mode,command.IsEnabled,command.RequireConfirmationForAmbiguousCategory,command.Aliases),audit,cancellationToken).ConfigureAwait(false);
        var runtime=await db.StoreVoiceCommandRuntimeSettings.SingleOrDefaultAsync(x=>x.TenantId==tenantId&&x.StoreId==storeId,cancellationToken).ConfigureAwait(false);var now=UtcNow();
        try{if(runtime is null){runtime=StoreVoiceCommandRuntimeSetting.Create(tenantId,storeId,command.LanguageCode,command.RequireConfirmation,command.ListeningTimeoutSeconds,command.MinimumRecognitionConfidence,now);db.StoreVoiceCommandRuntimeSettings.Add(runtime);}else runtime.Update(command.LanguageCode,command.RequireConfirmation,command.ListeningTimeoutSeconds,command.MinimumRecognitionConfidence,now);}catch(ArgumentException ex){throw new TenantBusinessRuleException(ex.Message);}
        RecordAudit(tenantId,storeId,audit,"StoreVoiceRuntimeSettingChanged","StoreVoiceCommandRuntimeSetting",storeId,before,new{command.LanguageCode,command.RequireConfirmation,command.ListeningTimeoutSeconds,command.MinimumRecognitionConfidence},now);await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);return await GetVoiceSettingAsync(storeId,cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<ProductCategoryAliasView>> ListCategoryAliasesAsync(long categoryId,long? storeId,CancellationToken cancellationToken=default)
    {
        var tenantId=RequireTenantId();var category=await RequireCategoryAsync(categoryId,storeId,cancellationToken).ConfigureAwait(false);
        if(storeId.HasValue)await RequireAuthorizedStoreAsync(storeId.Value,cancellationToken).ConfigureAwait(false);else if(!IsTenantWide())throw new TenantResourceNotFoundException("Category aliases");
        var query=db.ProductCategoryAliases.AsNoTracking().Where(x=>x.TenantId==tenantId&&x.ProductCategoryId==category.Id&&x.IsActive);
        if(storeId.HasValue)query=query.Where(x=>x.StoreId==null||x.StoreId==storeId.Value);
        return await query.OrderBy(x=>x.AliasText).Select(x=>new ProductCategoryAliasView(x.Id,x.StoreId,x.ProductCategoryId,x.AliasText,x.NormalizedAliasText,x.LanguageCode,x.IsActive,x.CreatedUtc)).ToArrayAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<ProductCategoryAliasView> AddCategoryAliasAsync(long categoryId,SaveProductCategoryAliasCommand command,TenantAuditContext audit,CancellationToken cancellationToken=default)
    {
        ArgumentNullException.ThrowIfNull(command);ValidateAudit(audit);var tenantId=RequireTenantId();_ = await RequireCategoryAsync(categoryId,command.StoreId,cancellationToken).ConfigureAwait(false);
        if(command.StoreId.HasValue)await RequireAuthorizedStoreAsync(command.StoreId.Value,cancellationToken).ConfigureAwait(false);else if(!IsTenantWide())throw new TenantResourceNotFoundException("Category");
        var normalized=ProductCategoryAlias.Normalize(command.AliasText);if(string.IsNullOrWhiteSpace(normalized))throw new TenantBusinessRuleException("Category alias is required.");
        if(await db.ProductCategoryAliases.AsNoTracking().AnyAsync(x=>x.TenantId==tenantId&&x.ProductCategoryId==categoryId&&x.StoreId==command.StoreId&&x.NormalizedAliasText==normalized&&x.IsActive,cancellationToken).ConfigureAwait(false))throw new TenantBusinessRuleException("Category alias already exists for this scope.");
        var now=UtcNow();ProductCategoryAlias alias;try{alias=ProductCategoryAlias.Create(tenantId,command.StoreId,categoryId,command.AliasText,command.LanguageCode,audit.ActorUserId,now);}catch(ArgumentException ex){throw new TenantBusinessRuleException(ex.Message);}
        db.ProductCategoryAliases.Add(alias);RecordAudit(tenantId,command.StoreId,audit,"ProductCategoryAliasAdded","ProductCategoryAlias",0,null,new{categoryId,command.StoreId,command.AliasText,command.LanguageCode},now);await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return MapAlias(alias);
    }

    public async Task<VoiceSessionView> StartVoiceSessionAsync(StartVoiceSessionCommand command,TenantAuditContext audit,CancellationToken cancellationToken=default)
    {
        ArgumentNullException.ThrowIfNull(command);ValidateAudit(audit);var tenantId=RequireTenantId();await RequireCustomerAtStoreAsync(command.CustomerId,command.StoreId,cancellationToken).ConfigureAwait(false);var setting=await GetVoiceSettingAsync(command.StoreId,cancellationToken).ConfigureAwait(false);if(!setting.IsEnabled)throw new TenantBusinessRuleException("Voice commands are disabled for this store.");
        var valid=string.Equals(setting.TriggerKeyword.Trim(),command.TriggerText.Trim(),StringComparison.OrdinalIgnoreCase)||setting.Aliases.Any(x=>string.Equals(x.Trim(),command.TriggerText.Trim(),StringComparison.OrdinalIgnoreCase));if(!valid)throw new TenantBusinessRuleException("Voice trigger does not match this store configuration.");
        var now=UtcNow();var session=VoiceCommandSession.Start(tenantId,command.StoreId,currentUser.UserId,command.CustomerId,command.TriggerText.Trim(),setting.RequireConfirmation,now,now.AddSeconds(setting.ListeningTimeoutSeconds));db.VoiceCommandSessions.Add(session);RecordAudit(tenantId,command.StoreId,audit,"VoiceCommandStarted","VoiceCommandSession",0,null,new{command.CustomerId,command.TriggerText,setting.LanguageCode},now);await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);return MapVoiceSession(session);
    }

    public async Task<VoiceInterpretResult> InterpretVoiceSessionAsync(long sessionId,InterpretVoiceSessionCommand command,TenantAuditContext audit,CancellationToken cancellationToken=default)
    {
        ArgumentNullException.ThrowIfNull(command);ValidateAudit(audit);var session=await RequireVoiceSessionAsync(sessionId,cancellationToken).ConfigureAwait(false);var setting=await GetVoiceSettingAsync(session.StoreId,cancellationToken).ConfigureAwait(false);var now=UtcNow();
        if(command.RecognitionConfidence<setting.MinimumRecognitionConfidence){RecordAudit(session.TenantId,session.StoreId,audit,"VoiceCommandRejectedLowConfidence","VoiceCommandSession",session.Id,null,new{command.RecognitionConfidence,setting.MinimumRecognitionConfidence},now);await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);throw new TenantBusinessRuleException("Voice recognition confidence is below the store threshold.");}
        var categoryText=ExtractCategoryText(command.RecognizedText,session.MatchedTrigger);if(string.IsNullOrWhiteSpace(categoryText)){RecordAudit(session.TenantId,session.StoreId,audit,"VoiceCommandCategoryNotFound","VoiceCommandSession",session.Id,null,new{command.RecognizedText},now);await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);throw new TenantBusinessRuleException("Category not found.");}
        var candidates=await ResolveCategoryCandidatesAsync(session.TenantId,session.StoreId,categoryText,cancellationToken).ConfigureAwait(false);
        if(candidates.Count==0){RecordAudit(session.TenantId,session.StoreId,audit,"VoiceCommandCategoryNotFound","VoiceCommandSession",session.Id,null,new{command.RecognizedText,CategoryText=categoryText},now);await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);throw new TenantBusinessRuleException("Category not found. Add an authorized category/alias instead of creating one from voice text.");}
        VoiceCategoryCandidate? selected=null;
        if(command.SelectedCategoryId.HasValue)
        {
            selected=candidates.SingleOrDefault(x=>x.CategoryId==command.SelectedCategoryId.Value);if(selected is null)throw new TenantBusinessRuleException("Selected category is not one of the server-resolved candidates.");
        }
        else if(candidates.Count==1) selected=candidates[0];
        if(selected is null)
        {
            RecordAudit(session.TenantId,session.StoreId,audit,"VoiceCommandCategoryAmbiguous","VoiceCommandSession",session.Id,null,new{command.RecognizedText,CategoryText=categoryText,Candidates=candidates.Select(x=>x.CategoryId).ToArray()},now);await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return new(MapVoiceSession(session),true,candidates,"Multiple categories matched. Select one of the server-resolved categories before any CRM write.");
        }
        try{session.Propose(command.RecognizedText,command.RecognitionConfidence,PreferenceType.Category,selected.CategoryId,selected.CategoryName,now);}catch(InvalidOperationException ex){throw new TenantBusinessRuleException(ex.Message);}
        RecordAudit(session.TenantId,session.StoreId,audit,"VoiceCommandInterpreted","VoiceCommandSession",session.Id,null,new{command.RecognizedText,command.RecognitionConfidence,SelectedCategoryId=selected.CategoryId,selected.CategoryName,session.ConfirmationRequired,session.Status},now);
        if(session.Status==VoiceCommandSessionStatus.Confirmed)CreateConfirmedVoiceSignal(session,command.Reason,audit.ActorUserId,now);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);return new(MapVoiceSession(session),false,[selected],session.Status==VoiceCommandSessionStatus.PendingConfirmation?"Category resolved. CRM write requires confirmation.":"Category resolved and confirmed by store policy.");
    }

    public async Task<VoiceSessionView> ConfirmVoiceSessionAsync(long sessionId,TenantAuditContext audit,CancellationToken cancellationToken=default)
    {
        ValidateAudit(audit);var session=await RequireVoiceSessionAsync(sessionId,cancellationToken).ConfigureAwait(false);var now=UtcNow();try{session.Confirm(now);}catch(InvalidOperationException ex){throw new TenantBusinessRuleException(ex.Message);}
        CreateConfirmedVoiceSignal(session,"Confirmed voice command",audit.ActorUserId,now);RecordAudit(session.TenantId,session.StoreId,audit,"VoiceCommandConfirmed","VoiceCommandSession",session.Id,null,new{session.CustomerId,session.ProposedPreferenceType,session.ProposedReferenceId,session.ProposedValue},now);await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);return MapVoiceSession(session);
    }

    public async Task<VoiceSessionView> RejectVoiceSessionAsync(long sessionId,TenantAuditContext audit,CancellationToken cancellationToken=default)
    {
        ValidateAudit(audit);var session=await RequireVoiceSessionAsync(sessionId,cancellationToken).ConfigureAwait(false);var now=UtcNow();try{session.Reject(now);}catch(InvalidOperationException ex){throw new TenantBusinessRuleException(ex.Message);}RecordAudit(session.TenantId,session.StoreId,audit,"VoiceCommandRejected","VoiceCommandSession",session.Id,null,new{session.CustomerId,session.ProposedValue},now);await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);return MapVoiceSession(session);
    }

    public async Task<IReadOnlyList<PreferenceAuditItem>> GetAuditHistoryAsync(long? customerId,long? storeId,int take=100,CancellationToken cancellationToken=default)
    {
        var tenantId=RequireTenantId();take=Math.Clamp(take,1,200);if(storeId.HasValue)await RequireAuthorizedStoreAsync(storeId.Value,cancellationToken).ConfigureAwait(false);if(customerId.HasValue)_=await RequireVisibleCustomerAsync(customerId.Value,cancellationToken).ConfigureAwait(false);
        var query=db.AuditLogs.AsNoTracking().Where(x=>x.TenantId==tenantId&&(x.Action.StartsWith("CustomerPreference")||x.Action.StartsWith("HouseholdPreference")||x.Action.StartsWith("PreferenceWeight")||x.Action.StartsWith("VoiceCommand")||x.Action.StartsWith("StoreVoice")||x.Action.StartsWith("ProductCategoryAlias")));
        if(storeId.HasValue)query=query.Where(x=>x.StoreId==storeId.Value);
        else if(!IsTenantWide()){var allowedStoreIds=currentUser.StoreIds.ToArray();query=query.Where(x=>x.StoreId==null||(x.StoreId.HasValue&&allowedStoreIds.Contains(x.StoreId.Value)));}
        if(customerId.HasValue){var id=customerId.Value.ToString(CultureInfo.InvariantCulture);query=query.Where(x=>x.EntityId==id||x.AfterJson!=null&&x.AfterJson.Contains($"\"CustomerId\":{customerId.Value}"));}
        return await query.OrderByDescending(x=>x.CreatedUtc).Take(take).Select(x=>new PreferenceAuditItem(x.Id,x.StoreId,x.UserId,x.Action,x.EntityType,x.EntityId,x.BeforeJson,x.AfterJson,x.CorrelationId,x.CreatedUtc)).ToArrayAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<IReadOnlyList<VoiceCategoryCandidate>> ResolveCategoryCandidatesAsync(long tenantId,long storeId,string categoryText,CancellationToken cancellationToken)
    {
        var normalized=ProductCategoryAlias.Normalize(categoryText);
        var categories=await db.ProductCategories.AsNoTracking().Where(x=>x.TenantId==tenantId&&x.IsActive&&(x.StoreId==null||x.StoreId==storeId)).ToArrayAsync(cancellationToken).ConfigureAwait(false);
        var aliases=await db.ProductCategoryAliases.AsNoTracking().Where(x=>x.TenantId==tenantId&&x.IsActive&&(x.StoreId==null||x.StoreId==storeId)).ToArrayAsync(cancellationToken).ConfigureAwait(false);
        var byId=categories.ToDictionary(x=>x.Id);
        var matches=new Dictionary<long,VoiceCategoryCandidate>();
        foreach(var category in categories)
        {
            var name=ProductCategoryAlias.Normalize(category.Name);var code=ProductCategoryAlias.Normalize(category.CategoryCode);
            if(name==normalized||code==normalized)matches[category.Id]=new(category.Id,category.CategoryCode,category.Name,"Category");
        }
        foreach(var alias in aliases)
            if(alias.NormalizedAliasText==normalized&&byId.TryGetValue(alias.ProductCategoryId,out var category))matches[category.Id]=new(category.Id,category.CategoryCode,category.Name,"Alias");
        if(matches.Count==0)
        {
            foreach(var category in categories)
            {
                var name=ProductCategoryAlias.Normalize(category.Name);if(name.Contains(normalized,StringComparison.Ordinal)||normalized.Contains(name,StringComparison.Ordinal))matches[category.Id]=new(category.Id,category.CategoryCode,category.Name,"CategoryPartial");
            }
            foreach(var alias in aliases)
                if((alias.NormalizedAliasText.Contains(normalized,StringComparison.Ordinal)||normalized.Contains(alias.NormalizedAliasText,StringComparison.Ordinal))&&byId.TryGetValue(alias.ProductCategoryId,out var category))matches[category.Id]=new(category.Id,category.CategoryCode,category.Name,"AliasPartial");
        }
        return matches.Values.OrderBy(x=>x.CategoryName).ToArray();
    }

    private async Task<ProductCategory> RequireCategoryAsync(long categoryId,long? storeId,CancellationToken cancellationToken)
    {
        var tenantId=RequireTenantId();var category=await db.ProductCategories.AsNoTracking().SingleOrDefaultAsync(x=>x.TenantId==tenantId&&x.Id==categoryId&&x.IsActive,cancellationToken).ConfigureAwait(false)??throw new TenantResourceNotFoundException("Category");
        if(storeId.HasValue&&category.StoreId.HasValue&&category.StoreId.Value!=storeId.Value)throw new TenantResourceNotFoundException("Category");return category;
    }

    private static string ExtractCategoryText(string recognizedText,string matchedTrigger)
    {
        if(string.IsNullOrWhiteSpace(recognizedText))return string.Empty;var text=recognizedText.Trim();var trigger=matchedTrigger.Trim();
        if(text.StartsWith(trigger,StringComparison.OrdinalIgnoreCase)){text=text[trigger.Length..].TrimStart(' ',':','-');}return text.Trim();
    }

    private async Task SyncPurchaseSignalsAsync(long customerId,long actorUserId,CancellationToken cancellationToken)
    {
        var tenantId=RequireTenantId();var allowedStatuses=new[]{RetailInvoiceStatus.Finalized,RetailInvoiceStatus.PartiallyPaid,RetailInvoiceStatus.Paid};
        var purchases=await(from i in db.RetailInvoices.AsNoTracking() join item in db.RetailInvoiceItems.AsNoTracking() on i.Id equals item.InvoiceId where i.TenantId==tenantId&&i.CustomerId==customerId&&allowedStatuses.Contains(i.Status) select new{i.StoreId,i.InvoiceUtc,ItemId=item.Id,item.ProductId,item.CategoryId,item.ProductNameSnapshot,item.CategoryNameSnapshot}).ToArrayAsync(cancellationToken).ConfigureAwait(false);
        var existingReasons=await db.CustomerPreferenceSignals.AsNoTracking().Where(x=>x.TenantId==tenantId&&x.CustomerId==customerId&&x.Source==PreferenceSignalSource.Purchase&&x.Reason!=null).Select(x=>x.Reason!).ToArrayAsync(cancellationToken).ConfigureAwait(false);var reasonSet=existingReasons.ToHashSet(StringComparer.Ordinal);
        foreach(var p in purchases)
        {
            if(p.CategoryId.HasValue){var reason=$"RetailInvoiceItem:{p.ItemId}:Category";if(reasonSet.Add(reason))db.CustomerPreferenceSignals.Add(CustomerPreferenceSignal.Create(tenantId,p.StoreId,customerId,PreferenceType.Category,p.CategoryId,p.CategoryNameSnapshot,100m,PreferenceSignalSource.Purchase,100m,actorUserId,reason,p.InvoiceUtc));}
            if(p.ProductId.HasValue){var reason=$"RetailInvoiceItem:{p.ItemId}:Product";if(reasonSet.Add(reason))db.CustomerPreferenceSignals.Add(CustomerPreferenceSignal.Create(tenantId,p.StoreId,customerId,PreferenceType.Product,p.ProductId,p.ProductNameSnapshot,100m,PreferenceSignalSource.Purchase,100m,actorUserId,reason,p.InvoiceUtc));}
        }
        if(purchases.Length>0)await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private void CreateConfirmedVoiceSignal(VoiceCommandSession session,string? reason,long actorUserId,DateTime now)
    {
        if(session.ProposedPreferenceType is null)throw new TenantBusinessRuleException("Voice session has no preference proposal.");var duplicate=db.CustomerPreferenceSignals.Local.Any(x=>x.TenantId==session.TenantId&&x.CustomerId==session.CustomerId&&x.Source==PreferenceSignalSource.VoiceConfirmed&&x.Reason!=null&&x.Reason.StartsWith($"VoiceSession:{session.Id}",StringComparison.Ordinal));if(duplicate)return;
        db.CustomerPreferenceSignals.Add(CustomerPreferenceSignal.Create(session.TenantId,session.StoreId,session.CustomerId,session.ProposedPreferenceType.Value,session.ProposedReferenceId,session.ProposedValue,100m,PreferenceSignalSource.VoiceConfirmed,session.RecognitionConfidence,actorUserId,$"VoiceSession:{session.Id}{(string.IsNullOrWhiteSpace(reason)?string.Empty:$"; {reason}")}",now));
    }

    private async Task<Customer> RequireVisibleCustomerAsync(long customerId,CancellationToken cancellationToken)
    {
        var tenantId=RequireTenantId();var customer=await db.Customers.AsNoTracking().SingleOrDefaultAsync(x=>x.TenantId==tenantId&&x.Id==customerId&&x.IsActive,cancellationToken).ConfigureAwait(false)??throw new TenantResourceNotFoundException("Customer");if(!IsTenantWide()){var visible=await db.CustomerStoreAssignments.AsNoTracking().AnyAsync(x=>x.TenantId==tenantId&&x.CustomerId==customerId&&currentUser.StoreIds.Contains(x.StoreId),cancellationToken).ConfigureAwait(false);if(!visible)throw new TenantResourceNotFoundException("Customer");}return customer;
    }

    private async Task RequireCustomerAtStoreAsync(long customerId,long storeId,CancellationToken cancellationToken)
    {
        await RequireAuthorizedStoreAsync(storeId,cancellationToken).ConfigureAwait(false);var tenantId=RequireTenantId();if(!await db.CustomerStoreAssignments.AsNoTracking().AnyAsync(x=>x.TenantId==tenantId&&x.CustomerId==customerId&&x.StoreId==storeId,cancellationToken).ConfigureAwait(false))throw new TenantResourceNotFoundException("Customer");
    }

    private async Task RequireAuthorizedStoreAsync(long storeId,CancellationToken cancellationToken)
    {
        var tenantId=RequireTenantId();if(!PhaseSixAccessRules.CanAccessStore(storeId,currentUser.StoreIds,IsTenantWide()))throw new TenantResourceNotFoundException("Store");if(!await db.Stores.AsNoTracking().AnyAsync(x=>x.TenantId==tenantId&&x.Id==storeId&&x.IsActive,cancellationToken).ConfigureAwait(false))throw new TenantResourceNotFoundException("Store");
    }

    private async Task ValidateReferenceAsync(PreferenceType type,long? referenceId,string? value,long? storeId,CancellationToken cancellationToken)
    {
        var tenantId=RequireTenantId();if(referenceId is null&&string.IsNullOrWhiteSpace(value))throw new TenantBusinessRuleException("Preference requires a reference or value.");
        if(type==PreferenceType.Category&&referenceId.HasValue){var valid=await db.ProductCategories.AsNoTracking().AnyAsync(x=>x.TenantId==tenantId&&x.Id==referenceId&&x.IsActive&&(!storeId.HasValue||x.StoreId==null||x.StoreId==storeId),cancellationToken).ConfigureAwait(false);if(!valid)throw new TenantBusinessRuleException("Category is invalid or outside the selected store scope.");}
        if(type==PreferenceType.Product&&referenceId.HasValue){var valid=await db.Products.AsNoTracking().AnyAsync(x=>x.TenantId==tenantId&&x.Id==referenceId&&x.IsActive,cancellationToken).ConfigureAwait(false);if(!valid)throw new TenantBusinessRuleException("Product is invalid for this tenant.");}
    }

    private async Task<VoiceCommandSession> RequireVoiceSessionAsync(long sessionId,CancellationToken cancellationToken)
    {
        var tenantId=RequireTenantId();var session=await db.VoiceCommandSessions.SingleOrDefaultAsync(x=>x.TenantId==tenantId&&x.Id==sessionId,cancellationToken).ConfigureAwait(false)??throw new TenantResourceNotFoundException("Voice command session");if(session.StaffUserId!=currentUser.UserId)throw new TenantResourceNotFoundException("Voice command session");await RequireAuthorizedStoreAsync(session.StoreId,cancellationToken).ConfigureAwait(false);return session;
    }

    private async Task<PreferenceWeightVersion> EnsureActiveWeightVersionAsync(long actorUserId,CancellationToken cancellationToken)
    {
        var tenantId=RequireTenantId();var current=await db.PreferenceWeightVersions.FirstOrDefaultAsync(x=>x.TenantId==tenantId&&x.IsActive,cancellationToken).ConfigureAwait(false);if(current is not null)return current;var created=PreferenceWeightVersion.Create(tenantId,"DEFAULT-1",1m,.8m,.5m,1m,actorUserId>0?actorUserId:currentUser.UserId,UtcNow());db.PreferenceWeightVersions.Add(created);await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);return created;
    }

    private async Task<CustomerPreferencesView> MapCustomerPreferencesAsync(Customer customer,CancellationToken cancellationToken)
    {
        var signals=await db.CustomerPreferenceSignals.AsNoTracking().Where(x=>x.TenantId==customer.TenantId&&x.CustomerId==customer.Id).OrderByDescending(x=>x.LastObservedUtc).Select(x=>new PreferenceSignalView(x.Id,x.StoreId,x.PreferenceType,x.ReferenceId,x.Value,x.SignalScore,x.Source,x.Confidence,x.FirstObservedUtc,x.LastObservedUtc,x.IsActive,x.Reason)).ToArrayAsync(cancellationToken).ConfigureAwait(false);
        var rawScores=await db.CustomerPreferenceScores.AsNoTracking().Where(x=>x.TenantId==customer.TenantId&&x.CustomerId==customer.Id).Select(x=>new PreferenceScoreView(x.Id,x.PreferenceType,x.ReferenceId,x.Value,x.Score,x.WeightVersionId,x.CalculatedUtc)).ToArrayAsync(cancellationToken).ConfigureAwait(false);
        var scores=rawScores.OrderByDescending(x=>x.Score).ThenBy(x=>x.Id).ToArray();return new(customer.Id,customer.CustomerCode,DisplayName(customer),signals,scores);
    }

    private static ProductCategoryAliasView MapAlias(ProductCategoryAlias x)=>new(x.Id,x.StoreId,x.ProductCategoryId,x.AliasText,x.NormalizedAliasText,x.LanguageCode,x.IsActive,x.CreatedUtc);
    private static PreferenceScoreView MapScore(CustomerPreferenceScore x)=>new(x.Id,x.PreferenceType,x.ReferenceId,x.Value,x.Score,x.WeightVersionId,x.CalculatedUtc);
    private static PreferenceWeightView MapWeight(PreferenceWeightVersion x)=>new(x.Id,x.VersionCode,x.ManualStaffWeight,x.PurchaseWeight,x.CategoryInteractionWeight,x.VoiceConfirmedWeight,x.IsActive,x.CreatedUtc);
    private static VoiceSessionView MapVoiceSession(VoiceCommandSession x)=>new(x.Id,x.StoreId,x.CustomerId,x.MatchedTrigger,x.RecognizedText,x.RecognitionConfidence,x.ProposedPreferenceType,x.ProposedReferenceId,x.ProposedValue,x.ConfirmationRequired,x.Status,x.ExpiresUtc,x.ResolvedUtc);
    private static string DisplayName(Customer c)=>string.Join(' ',new[]{c.FirstName,c.LastName}.Where(x=>!string.IsNullOrWhiteSpace(x)));
    private static decimal WeightFor(PreferenceWeightVersion w,PreferenceSignalSource source)=>source switch{PreferenceSignalSource.ManualStaff=>w.ManualStaffWeight,PreferenceSignalSource.Purchase=>w.PurchaseWeight,PreferenceSignalSource.CategoryInteraction=>w.CategoryInteractionWeight,PreferenceSignalSource.VoiceConfirmed=>w.VoiceConfirmedWeight,_=>0};
    private bool IsTenantWide()=>PhaseSixAccessRules.IsTenantWide(currentUser.Roles);
    private long RequireTenantId()=>currentUser.TenantId is>0 and var id?id:throw new UnauthorizedAccessException("Tenant context is required.");
    private DateTime UtcNow()=>timeProvider.GetUtcNow().UtcDateTime;
    private static void ValidateAudit(TenantAuditContext audit){if(audit.ActorUserId<=0||string.IsNullOrWhiteSpace(audit.CorrelationId))throw new ArgumentException("Valid audit context is required.",nameof(audit));}
    private void RecordAudit(long tenantId,long? storeId,TenantAuditContext audit,string action,string entityType,long entityId,object? before,object? after,DateTime now)=>db.AuditLogs.Add(AuditLog.Record(tenantId,storeId,audit.ActorUserId,"User",action,entityType,entityId==0?null:entityId.ToString(CultureInfo.InvariantCulture),before is null?null:JsonSerializer.Serialize(before),after is null?null:JsonSerializer.Serialize(after),audit.IpAddress,audit.UserAgent,audit.CorrelationId,now));
}
