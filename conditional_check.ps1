$file = "d:\apps\PPR-main (3)\PPR-main\Login2_copie\Veiw\Admin\ThesisView.xaml.cs"
$content = Get-Content -Path $file -Raw
$lines = Get-Content -Path $file

$ifCount = 0
$endifCount = 0

for($i = 0; $i -lt $lines.Count; $i++) {
    $line = $lines[$i]
    if($line -match "#if\s+WINDOWS") {
        $ifCount++
        Write-Host "Line $($i+1): #if WINDOWS (Level: $ifCount)"
    }
    if($line -match "#endif") {
        $endifCount++
        Write-Host "Line $($i+1): #endif (Level: $ifCount)"
        $ifCount--
    }
}

Write-Host "`nSummary:"
Write-Host "Total #if WINDOWS: $($ifCount + $endifCount)"
Write-Host "Total #endif: $endifCount"
Write-Host "Missing #endif directives: $ifCount"
