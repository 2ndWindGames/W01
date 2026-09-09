$ErrorActionPreference = 'Stop'
Add-Type -Path "$PSScriptRoot/../Assets/01.Scripts/Manager/InterstitialSchedule.cs"
$schedule = New-Object _01.Scripts.Manager.InterstitialSchedule
function Assert-Ads($condition, $message) { if (-not $condition) { throw $message } }
$schedule.RecordRound()
$schedule.RecordRound()
Assert-Ads (-not $schedule.CanShow(60)) 'Must not show before three completed rounds'
$schedule.RecordRound()
Assert-Ads ($schedule.CanShow(90)) 'First ad must be eligible after third round'
Assert-Ads ($schedule.CanShow(100)) 'Unavailable ad must retain eligibility'
$schedule.MarkShown(100)
Assert-Ads ($schedule.CompletedRounds -eq 0) 'Successful open must reset rounds'
1..3 | ForEach-Object { $schedule.RecordRound() }
Assert-Ads (-not $schedule.CanShow(279.9)) 'Cooldown must last 180 seconds'
Assert-Ads ($schedule.CanShow(280)) 'Exactly 180 seconds must be eligible'
$schedule.RecordRound()
Assert-Ads ($schedule.CanShow(281)) 'Rounds during cooldown must remain counted'
$schedule.MarkShown(281)
Assert-Ads (-not $schedule.CanShow(1000)) 'Elapsed time alone must not show an ad'
Write-Output 'PASS: first 3 rounds, unavailable ad, reset on show, cooldown boundary, retained rounds.'
