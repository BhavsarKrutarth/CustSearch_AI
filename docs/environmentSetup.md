cd "D:\Project\AdminCore\CustSearch_AI\CustSearch_AI"

# Database
$env:ConnectionStrings__CustSearchDatabase = 'Server=KRUTARTH-BHAVSA;Database=CustSearch_AI;Integrated Security=True;Encrypt=True;TrustServerCertificate=True'

# Generate JWT signing key
$bytes = New-Object byte[] 48
$rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
$rng.GetBytes($bytes)
$env:Jwt__SigningKey = [Convert]::ToBase64String($bytes)
$rng.Dispose()

# Generate DIFFERENT report download signing key
$bytes = New-Object byte[] 48
$rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
$rng.GetBytes($bytes)
$env:ReportsExports__DownloadSigningKey = [Convert]::ToBase64String($bytes)
$rng.Dispose()

Write-Host "Database:"
Write-Host $env:ConnectionStrings__CustSearchDatabase

Write-Host "JWT key length:"
Write-Host $env:Jwt__SigningKey.Length

Write-Host "Reports key length:"
Write-Host $env:ReportsExports__DownloadSigningKey.Length


only information : 
$JwtKey : BAc2thoVRu9qTwC4gSTmnkU113qfiNAEjSL9J/rW44VYx4qYINaLC2uspQsnHNhV
Jwt__SigningKey : ug2TN4aWcf8oN2PLeiQZ1o+u27FAb6NNZsQiCrD4qDS4t/GJ2G+9m49JfY1Nw0kv