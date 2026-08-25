using CustSearch.Application.Operations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CustSearch.API.Operations;

public sealed class OperationalExceptionFilter:IExceptionFilter{public void OnException(ExceptionContext context){if(context.Exception is not OperationalException exception)return;var status=exception.Kind switch{OperationalFailureKind.Validation=>400,OperationalFailureKind.Forbidden=>403,OperationalFailureKind.NotFound=>404,OperationalFailureKind.Conflict=>409,OperationalFailureKind.Unavailable=>503,_=>500};context.Result=new ObjectResult(new ProblemDetails{Status=status,Title="Operational platform request failed",Detail=exception.Message}){StatusCode=status};context.ExceptionHandled=true;}}

