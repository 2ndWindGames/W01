"""Build bilingual Neon Touch Reels and Shorts from shipped art and real Unity captures.

Requires Pillow, numpy, ffmpeg, and edge-tts. Speech synthesis sends only the
approved narration lines to Microsoft's speech endpoint; no project files.
"""
from __future__ import annotations

import argparse
import asyncio
import json
import math
from pathlib import Path
import subprocess
import sys
import wave

import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageFont

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "SocialMedia"
WORK = OUT / "work"
VOICE = WORK / "voice"
for directory in (OUT, WORK, VOICE):
    directory.mkdir(parents=True, exist_ok=True)

# W13's existing production runtime is read-only. Supply FFMPEG and TTS_PYTHON
# environment variables to use standalone installations instead.
W13_PY = Path(r"C:\SecondWindGames\Repositoires\Unity\W13\BuildArtifacts\Promo\python")
if W13_PY.exists():
    sys.path.insert(0, str(W13_PY))
import edge_tts
import imageio_ffmpeg

FFMPEG = imageio_ffmpeg.get_ffmpeg_exe()
WIDTH, HEIGHT, FPS = 720, 1280, 24
CAPTURE = ROOT / "StoreListing"
BACKGROUND = Image.open(CAPTURE / "source/feature_energy_background.png").convert("RGB")
TARGETS = ROOT / "Assets/Resources/UI/NeonSignalPack/Targets"
MUSIC = ROOT / "Assets/Resources/Sounds/BGM/Gameplay_NeonRush.wav"
TAP = ROOT / "Assets/Resources/Sounds/SFX/Target_NeonTap.wav"
FONT = ROOT / "Assets/Resources/Fonts/NotoSansKR-Variable.ttf"

COPY = {
 "ko": {
  "instagram": [
   ("hook",0,4,"네온터치","손끝으로 네온을 깨워!","딱 한 번의 터치로, 네온을 깨워 봐!"),
   ("tap",4,9,"빛나는 타겟을 콕!","간단하지만 손이 바빠지는 순간","빛나는 타겟을 콕! 빠르게, 정확하게!"),
   ("combo",9,14,"콤보를 이어가!","리듬을 놓치지 마","연속 터치로 콤보를 쌓아 봐. 좋아, 계속!"),
   ("fever",14,19,"피버 타임!","몰입감이 터지는 순간","이제 피버 타임! 손끝의 속도를 올려!"),
   ("end",19,24,"30초의 도전","내 최고 기록, 넘어볼까?","단 삼십 초. 네온터치에서 내 기록에 도전해 봐!"),
  ],
  "shorts": [
   ("hook",0,4,"잠깐, 이 빛 봤어?","네온터치 · 원터치 챌린지","이 빛 봤어? 네온터치, 시작!"),
   ("tap",4,10,"타겟을 찾아 콕!","정확한 한 번이 시작","화면에 뜬 네온을 찾아, 콕! 좋아!"),
   ("targets",10,16,"색다른 타겟","순간마다 달라지는 선택","빠른 타겟, 특별한 타겟. 다음은 뭐가 나올까?"),
   ("combo",16,22,"연속 터치!","콤보를 끊지 마","하나, 둘, 셋! 리듬을 타면 콤보가 올라가!"),
   ("fever",22,28,"피버가 터진다!","네온의 에너지를 느껴 봐","지금이야, 피버! 더 빠르게, 더 짜릿하게!"),
   ("best",28,34,"남은 시간은 30초","오늘의 최고 기록을 향해","시간은 짧아도 도전은 진짜야. 최고 기록, 갱신해 봐!"),
   ("end",34,39,"네온터치","다음 기록은 네 거야","네온터치. 다음 기록은, 네 거야!"),
  ],
 },
 "en": {
  "instagram": [
   ("hook",0,4,"VIOLET TAP","Wake the neon with one tap.","One tap. Wake up the neon!"),
   ("tap",4,9,"SPOT IT. TAP IT.","A split-second challenge","See it glow? Tap! Quick and clean."),
   ("combo",9,14,"KEEP THE COMBO","Find your rhythm","Keep tapping. Yes! Build that combo!"),
   ("fever",14,19,"FEVER TIME!","Feel the rush","Here comes fever! Pick up the pace!"),
   ("end",19,24,"30 SECONDS","Can you beat your best?","Thirty seconds. Beat your best in Violet Tap!"),
  ],
  "shorts": [
   ("hook",0,4,"WAIT—THAT GLOW!","Violet Tap · one-touch challenge","See that glow? Violet Tap starts now!"),
   ("tap",4,10,"FIND THE TARGET","One precise tap","Spot the neon target, then tap! Nice one!"),
   ("targets",10,16,"NEW TARGETS","A fresh choice every moment","Quick targets. Special targets. What will pop up next?"),
   ("combo",16,22,"KEEP IT GOING","Don't break your streak","One, two, three! Find the rhythm and build your combo!"),
   ("fever",22,28,"FEVER IS ON!","Feel that neon energy","There it is—fever! Faster now. Feel that rush!"),
   ("best",28,34,"30 SECONDS","Chase a new personal best","The clock is short, but the challenge is real. Can you beat your best?"),
   ("end",34,39,"VIOLET TAP","Your next record is waiting","Violet Tap. Your next record is waiting!"),
  ],
 },
}

SCREEN = {"hook":"01_intro.png","tap":"03_gameplay.png","targets":"03_gameplay.png",
          "combo":"03_gameplay.png","fever":"03_gameplay.png","best":"02_ready.png",
          "end":"01_intro.png"}
SPRITE = {"tap":"target_normal.png","targets":"target_time.png",
          "combo":"target_quick.png","fever":"target_overload.png"}

def font(size:int) -> ImageFont.FreeTypeFont:
    return ImageFont.truetype(str(FONT),size)

def fit(text:str,max_width:int,start:int) -> ImageFont.FreeTypeFont:
    size=start
    while size>28 and ImageDraw.Draw(Image.new("RGB",(1,1))).textbbox((0,0),text,font=font(size))[2]>max_width:
        size-=2
    return font(size)

def base_image(lang:str,key:str) -> Image.Image:
    locale = "ko-KR" if lang=="ko" else "en-US"
    screen = Image.open(CAPTURE / locale / "screenshots" / SCREEN[key]).convert("RGB")
    # The full untouched gameplay capture stays inside its own framed panel.
    phone = screen.resize((388,798),Image.Resampling.LANCZOS)
    art = BACKGROUND.resize((WIDTH,HEIGHT),Image.Resampling.LANCZOS).filter(ImageFilter.GaussianBlur(20))
    dark=Image.new("RGBA",(WIDTH,HEIGHT),(3,5,22,155))
    art=Image.alpha_composite(art.convert("RGBA"),dark)
    im=Image.new("RGBA",(WIDTH,HEIGHT),(5,7,25,255))
    im.alpha_composite(art)
    d=ImageDraw.Draw(im,"RGBA")
    d.rounded_rectangle((155,230,565,1048),radius=28,fill=(50,21,92,180),outline=(99,224,255,230),width=4)
    im.paste(phone,(166,240))
    d=ImageDraw.Draw(im,"RGBA")
    d.rounded_rectangle((164,238,556,1040),radius=8,outline=(174,99,255,210),width=2)
    if key in SPRITE:
        sprite=Image.open(TARGETS/SPRITE[key]).convert("RGBA")
        sprite.thumbnail((240,240),Image.Resampling.LANCZOS)
        x,y=475,740
        shadow=Image.new("RGBA",im.size)
        shadow.alpha_composite(sprite,(x-sprite.width//2,y-sprite.height//2))
        im.alpha_composite(shadow.filter(ImageFilter.GaussianBlur(24)))
        im.alpha_composite(sprite,(x-sprite.width//2,y-sprite.height//2))
    # Text plates are ad graphics outside the untouched screenshot.
    d=ImageDraw.Draw(im,"RGBA")
    d.rounded_rectangle((30,42,690,217),radius=26,fill=(5,8,31,230),outline=(150,75,243,175),width=2)
    d.rounded_rectangle((30,1070,690,1238),radius=26,fill=(5,8,31,233),outline=(46,211,255,170),width=2)
    return im

def scene_image(lang:str,profile:str,index:int) -> Image.Image:
    key,_,_,headline,subline,_=COPY[lang][profile][index]
    im=base_image(lang,key)
    d=ImageDraw.Draw(im)
    head=fit(headline,600,66 if lang=="en" else 63)
    sub=fit(subline,610,33)
    d.text((WIDTH/2,115),headline,font=head,anchor="mm",fill="#F9F3FF",stroke_width=2,stroke_fill="#5A1BB8")
    d.text((WIDTH/2,185),subline,font=sub,anchor="mm",fill="#82E5FF")
    counter=f"{index+1:02d} / {len(COPY[lang][profile]):02d}"
    d.text((WIDTH/2,1115),counter,font=font(26),anchor="mm",fill="#9EDFFE")
    footer = {
        "ko":{"hook":"SECONDWINDGAMES","tap":"빛을 터치해!","targets":"순간을 놓치지 마",
              "combo":"콤보를 이어가","fever":"속도를 올려!","best":"최고 기록 도전","end":"지금 플레이해 봐"},
        "en":{"hook":"SECONDWINDGAMES","tap":"TAP THE GLOW","targets":"STAY SHARP",
              "combo":"KEEP THE STREAK","fever":"FEEL THE RUSH","best":"BEAT YOUR BEST","end":"PLAY YOUR NEXT ROUND"},
    }
    callout=footer[lang][key]
    d.text((WIDTH/2,1182),callout,font=fit(callout,570,40),anchor="mm",fill="#F8FAFF")
    return im.convert("RGB")

async def voices(lang:str,profile:str) -> None:
    voice = "ko-KR-SunHiNeural" if lang=="ko" else "en-US-AriaNeural"
    sem=asyncio.Semaphore(3)
    async def make(index:int,line:str):
        dest=VOICE/f"{profile}-{lang}-{index:02}.mp3"
        if dest.exists() and dest.stat().st_size>2000:return
        async with sem:
            # Short takes allow the tempo and emotional shape to change at each cut.
            rate=["+7%","+3%","+5%","+8%","+2%","+5%","+3%"][index]
            pitch=["+5Hz","+2Hz","+3Hz","+7Hz","+1Hz","+3Hz","+1Hz"][index]
            await edge_tts.Communicate(line,voice,rate=rate,pitch=pitch).save(str(dest))
            print("Voice:",dest.name,flush=True)
    await asyncio.gather(*(make(i,s[5]) for i,s in enumerate(COPY[lang][profile])))

def decode(path:Path,rate:int=48000) -> np.ndarray:
    cmd=[FFMPEG,"-v","error","-i",str(path),"-f","f32le","-ac","1","-ar",str(rate),"-"]
    return np.frombuffer(subprocess.check_output(cmd),dtype="<f4").copy()

def soundtrack(lang:str,profile:str) -> Path:
    duration=COPY[lang][profile][-1][2]
    rate=48000;n=round(duration*rate)
    music=decode(MUSIC)
    music=np.tile(music,math.ceil(n/len(music)))[:n].copy()
    music*=.17
    mix=np.zeros((n,2),np.float32)
    voice=np.zeros(n,np.float32)
    duck=np.ones(n,np.float32)
    cue=[]
    for i,scene in enumerate(COPY[lang][profile]):
        key,start,end,_,_,line=scene
        y=decode(VOICE/f"{profile}-{lang}-{i:02}.mp3")
        active=np.flatnonzero(np.abs(y)>.002)
        if len(active):y=y[max(0,active[0]-3000):min(len(y),active[-1]+7500)]
        available=end-start-.65
        speed=max(1,len(y)/rate/available)
        if speed>1.0:
            # Never rush a read beyond 10%; leave a short tail or extend into the cut.
            speed=min(speed,1.10)
            cmd=[FFMPEG,"-v","error","-f","f32le","-ac","1","-ar",str(rate),"-i","-","-af",f"atempo={speed}","-f","f32le","-"]
            y=np.frombuffer(subprocess.run(cmd,input=y.astype("<f4").tobytes(),capture_output=True,check=True).stdout,dtype="<f4").copy()
        y=y[:round((end-start-.25)*rate)]
        rms=float(np.sqrt(np.mean(y*y))) if len(y) else 0
        y*=min(2.8,.12/max(.001,rms))
        pos=round((start+.28)*rate)
        voice[pos:pos+len(y)]+=y[:max(0,n-pos)]
        lo=max(0,pos-round(.12*rate));hi=min(n,pos+len(y)+round(.20*rate))
        duck[lo:hi]=.38
        cue.append({"start":round(pos/rate,3),"end":round(min(n,pos+len(y))/rate,3),"line":line,"speed":round(speed,3)})
    mix[:,0]=music*duck+voice
    mix[:,1]=music*duck+voice
    tap=decode(TAP)
    for st in ([4.3,9.3,14.2,19.2] if profile=="instagram" else [4.2,7.0,10.2,13.3,16.2,19.3,22.3,25.4,28.4,34.2]):
        pos=round(st*rate);clip=tap[:min(len(tap),n-pos)]
        mix[pos:pos+len(clip),0]+=clip*.10
        mix[pos:pos+len(clip),1]+=clip*.12
    fade=np.minimum(np.arange(n)/(rate*.2),1)*np.minimum((n-np.arange(n))/(rate*.6),1)
    mix*=fade[:,None]
    peak=np.max(np.abs(mix))
    if peak>.88:mix*=.88/peak
    dest=WORK/f"audio-{profile}-{lang}.wav"
    with wave.open(str(dest),"wb") as wav:
        wav.setnchannels(2);wav.setsampwidth(2);wav.setframerate(rate)
        wav.writeframes((mix*32767).astype("<i2").tobytes())
    def stamp(t:float)->str:
        ms=round(t*1000);return f"{ms//3600000:02}:{ms//60000%60:02}:{ms//1000%60:02},{ms%1000:03}"
    srt=[]
    for i,c in enumerate(cue,1):
        srt += [str(i),f"{stamp(c['start'])} --> {stamp(c['end'])}",c["line"],""]
    (OUT/f"NeonTouch-{profile}-{lang}.srt").write_text("\n".join(srt),encoding="utf-8-sig")
    (WORK/f"timing-{profile}-{lang}.json").write_text(json.dumps(cue,ensure_ascii=False,indent=2),encoding="utf-8")
    return dest

def render(lang:str,profile:str)->None:
    segments=COPY[lang][profile]
    duration=segments[-1][2]
    scenes=[scene_image(lang,profile,i) for i in range(len(segments))]
    cover=scenes[0].resize((1080,1920),Image.Resampling.LANCZOS)
    cover.save(OUT/f"NeonTouch-{profile}-{lang}-cover.jpg",quality=93)
    wav=soundtrack(lang,profile)
    temp=WORK/f"silent-{profile}-{lang}.mp4"
    cmd=[FFMPEG,"-hide_banner","-loglevel","error","-y","-f","rawvideo","-pix_fmt","rgb24",
         "-s",f"{WIDTH}x{HEIGHT}","-r",str(FPS),"-i","-","-vf","scale=1080:1920:flags=lanczos,format=yuv420p",
         "-an","-c:v","libx264","-preset","veryfast","-crf","19","-threads","4",
         "-color_primaries","bt709","-color_trc","bt709","-colorspace","bt709",str(temp)]
    proc=subprocess.Popen(cmd,stdin=subprocess.PIPE)
    for frame in range(duration*FPS):
        t=frame/FPS
        index=next(i for i,s in enumerate(segments) if s[1]<=t<s[2])
        key,start,end,*_=segments[index]
        base=scenes[index]
        # Gentle push-in; the image remains correctly framed at all times.
        phase=(t-start)/(end-start)
        scale=1.0+.022*phase
        crop_w=round(WIDTH/scale);crop_h=round(HEIGHT/scale)
        x=round((WIDTH-crop_w)/2+3*math.sin(t*.8))
        x=max(0,min(WIDTH-crop_w,x))
        y=round((HEIGHT-crop_h)/2)
        frame_image=base.crop((x,y,x+crop_w,y+crop_h)).resize((WIDTH,HEIGHT),Image.Resampling.BICUBIC)
        if 0<index and t-start<.13:
            previous=scenes[index-1]
            frame_image=Image.blend(previous,frame_image,(t-start)/.13)
        proc.stdin.write(frame_image.tobytes())
        if frame%(FPS*5)==0:print(profile,lang,frame//FPS,"/",duration,flush=True)
    proc.stdin.close()
    if proc.wait()!=0:raise RuntimeError("Video encoding failed")
    final=OUT/f"NeonTouch-{profile}-{lang}.mp4"
    cmd=[FFMPEG,"-hide_banner","-loglevel","error","-y","-i",str(temp),"-i",str(wav),
         "-map","0:v:0","-map","1:a:0","-c:v","copy","-c:a","aac","-b:a","192k",
         "-af","loudnorm=I=-16:TP=-1.5:LRA=9","-ar","48000","-movflags","+faststart",
         "-metadata",f"title=Neon Touch {profile} {lang}",
         "-metadata","artist=SecondWindGames",str(final)]
    subprocess.run(cmd,check=True)
    print("DONE",final,final.stat().st_size,flush=True)

async def main()->None:
    parser=argparse.ArgumentParser()
    parser.add_argument("--profile",choices=["instagram","shorts","all"],default="all")
    parser.add_argument("--lang",choices=["ko","en","all"],default="all")
    parser.add_argument("--voice-only",action="store_true")
    args=parser.parse_args()
    profiles=["instagram","shorts"] if args.profile=="all" else [args.profile]
    languages=["ko","en"] if args.lang=="all" else [args.lang]
    for profile in profiles:
        for lang in languages:
            await voices(lang,profile)
            if not args.voice_only:render(lang,profile)

if __name__=="__main__":
    asyncio.run(main())
