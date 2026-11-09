
$baseUrl = "http://localhost:8080"

$classId = Read-Host "Enter your Test Class ID"

$studentIds = @(
    "70000000000000001",
    "70000000000000002",
    "70000000000000003",
    "70000000000000004",
    "70000000000000005"
)

$jobs = @()

foreach ($studentId in $studentIds) {

    $jobs += Start-Job -ArgumentList $baseUrl, $classId, $studentId -ScriptBlock {

        param($baseUrl, $classId, $studentId)

        $body = @{
            classId = $classId
        } | ConvertTo-Json -Compress

        try {

            $response = Invoke-WebRequest `
                -UseBasicParsing `
                -Uri "$baseUrl/admission-holds" `
                -Method POST `
                -Headers @{
                    "X-Student-Id" = $studentId
                } `
                -ContentType "application/json" `
                -Body $body

            [PSCustomObject]@{
                StudentId = $studentId
                StatusCode = [int]$response.StatusCode
                Result = "SUCCESS"
            }

        }
        catch {

            $statusCode = 0

            if ($_.Exception.Response) {
                $statusCode = [int]$_.Exception.Response.StatusCode
            }

            [PSCustomObject]@{
                StudentId = $studentId
                StatusCode = $statusCode
                Result = "FAILED"
            }
        }
    }
}

$jobs | Wait-Job | Out-Null

$results = $jobs | Receive-Job

$jobs | Remove-Job

Write-Host "`nCONCURRENT TEST RESULTS" -ForegroundColor Cyan

$results | Format-Table StudentId, StatusCode, Result -AutoSize

$created = @($results | Where-Object {
    $_.StatusCode -eq 201
}).Count

$conflicts = @($results | Where-Object {
    $_.StatusCode -eq 409
}).Count

Write-Host "`nSuccessful Holds: $created"
Write-Host "Rejected Requests: $conflicts"

if ($created -eq 2 -and $conflicts -eq 3) {
    Write-Host "CONCURRENCY TEST PASSED!" -ForegroundColor Green
}
else {
    Write-Host "CHECK THE RESULTS!" -ForegroundColor Red
}
