# Nexus / Vortex 업로드 준비 가이드

## 지금 자동화된 것
GitHub Actions가 빌드 성공 시 아래 파일을 만듭니다.

`SwordMastery-<manifest Version>-Nexus-Vortex.zip`

ZIP 내부 구조:
```
SwordMastery/
├─ SwordMastery.dll
├─ manifest.json
├─ README_USER.txt
├─ assets/
└─ i18n/
```

이 구조는 SMAPI 수동 설치 시 `Mods/SwordMastery/manifest.json` 형태가 되고,
Vortex용 Nexus 배포 파일로도 쓰기 편한 구조입니다.

## Nexus에 올리기 전 마지막 TODO
1. `manifest.json`의 Author가 현재 `Prototype`입니다.
   공개용 제작자 이름으로 변경하세요.
2. `manifest.json`의 Version을 공개 버전에 맞추세요.
   예: 0.9.0(beta) 또는 1.0.0.
3. Nexus 페이지 생성 후 발급되는 Mod ID를 확인하세요.
4. 그 뒤 manifest에 아래를 추가하세요.
   `"UpdateKeys": ["Nexus:<MOD_ID>"]`
5. GitHub Actions를 다시 돌려 새 ZIP을 받으세요.
6. Nexus Files 탭에 그 ZIP을 Main File로 업로드하세요.

## Nexus 설정 권장
- Game: Stardew Valley
- Category: Gameplay Mechanics 또는 Skills 계열에 가장 가까운 카테고리
- Requirement: SMAPI
- Optional requirement: Generic Mod Config Menu
- Mod Manager Download: 허용
- File type: ZIP

## Vortex
별도 FOMOD 설치 스크립트는 필요하지 않습니다.
이 모드는 한 개의 SMAPI 모드 폴더만 배치하면 되므로 표준 ZIP 구조로 충분합니다.

## Android
Vortex는 PC용 워크플로입니다.
Android는 ZIP을 직접 압축 해제해 Android SMAPI의 Mods 폴더에 넣는 수동 설치 안내를 별도로 두는 편이 좋습니다.
