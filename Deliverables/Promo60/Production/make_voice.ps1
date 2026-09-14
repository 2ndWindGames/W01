$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$voiceDir = Join-Path $root 'Production/Voice'
New-Item -ItemType Directory -Path $voiceDir -Force | Out-Null

$scripts = [ordered]@{
    KO = @(
        '빛을 쫓아, 터치해!',
        '네온터치. 빛나는 타겟을 빠르고 정확하게 터치해.',
        '콤보를 이어가면, 열 번부터 점수가 두 배!',
        '피버가 시작되면 타겟이 더 많이 등장해.',
        '모래시계는 시간을 늘려 줘. 놓치지 마!',
        '폭탄은 피해! 건드리면 시간과 콤보를 잃어.',
        '게임이 진행될수록 점점 빨라져. 집중!',
        '숫자 타겟은 하나, 둘, 셋. 순서대로!',
        '오십 콤보부터 점수 세 배. 어디까지 갈 수 있을까?',
        '네온터치에 도전해 봐. 프로필에서 네온터치 다운로드 링크를 찾아 줘!'
    )
    EN = @(
        'Catch the light. Make the tap!',
        'Violet Tap. Find the neon and tap with precision.',
        'Keep your combo alive. Ten taps bring double points.',
        'Fever brings more targets onto the screen.',
        'An hourglass adds time. Do not let it slip away.',
        'Avoid the bomb! It costs time and breaks your combo.',
        'As the round goes on, the pace gets faster. Stay sharp!',
        'When numbers appear, tap one, two, three, in order.',
        'At fifty combo, your points triple. How far can you go?',
        'Try Violet Tap. Find the Violet Tap download link in our profile!'
    )
}

foreach ($locale in $scripts.Keys) {
    $synth = New-Object System.Speech.Synthesis.SpeechSynthesizer
    try {
        $synth.SelectVoice($(if ($locale -eq 'KO') { 'Microsoft Heami Desktop' } else { 'Microsoft Zira Desktop' }))
        $synth.Rate = 1
        $synth.Volume = 100
        for ($i = 0; $i -lt $scripts[$locale].Count; $i++) {
            $path = Join-Path $voiceDir ('{0}-{1:D2}.wav' -f $locale, $i)
            $synth.SetOutputToWaveFile($path)
            $synth.Speak($scripts[$locale][$i])
            $synth.SetOutputToNull()
        }
    } finally {
        $synth.Dispose()
    }
}
$scripts | ConvertTo-Json -Depth 3 | Set-Content -Encoding utf8 (Join-Path $voiceDir 'script.json')
