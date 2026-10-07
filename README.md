# SwordMastery v0.1

Stardew Valley 1.6 / SMAPI 검술 모드 프로토타입.

## GitHub에서 빌드하기

1. 새 GitHub 저장소를 만든다.
2. 이 ZIP 안의 **내용물 전체**를 저장소 루트에 올린다.
   - `ModEntry.cs`
   - `SwordMastery.csproj`
   - `manifest.json`
   - `Models/`
   - `Services/`
   - `assets/`
   - `i18n/`
   - `.github/workflows/build.yml`
3. Commit하면 `Actions` 탭에서 **Build SwordMastery**가 자동 실행된다.
4. 초록색 체크가 뜨면 실행 결과 페이지 맨 아래 `Artifacts`에서 **SwordMastery-Install**을 받는다.
5. Artifact ZIP을 한 번 풀면 `SwordMastery-Install.zip`이 나온다.
6. 그 ZIP을 다시 풀면 `SwordMastery/` 폴더가 나온다.
7. Android에서는 아래 위치에 넣는다.

`/storage/emulated/0/Android/data/abc.smapi.gameloader/files/Mods/SwordMastery/`

최종 형태:

```text
Mods/
└─ SwordMastery/
   ├─ SwordMastery.dll
   ├─ manifest.json
   ├─ assets/
   └─ i18n/
```

## 첫 테스트

게임을 실행한 뒤 SMAPI 로그에서 아래 문구를 찾는다.

`Sword Mastery prototype loaded.`

세이브를 불러오면 화면 왼쪽 위에 대략 아래처럼 표시된다.

`Sword Lv.0 EXP 0/40 SP:0`

몬스터를 잡으면 EXP가 오른다.

## 테스트 콘솔 명령

- `sm_status`
- `sm_add basic A`
- `sm_add basic B`
- `sm_add basic C`
- `sm_grant_ohgi`
- `sm_choose_ohgi A`
- `sm_choose_ohgi B`
- `sm_choose_ohgi C`
- `sm_add ohgi`
- `sm_grant_ultimate`
- `sm_choose_ultimate A`
- `sm_choose_ultimate B`
- `sm_add ultimate`
- `sm_reset`

지금 버전의 목적은 **PC/Android에서 로드 + 저장 + 킬 EXP + 성장 로직이 정상 작동하는지 먼저 확인**하는 것이다.
실제 검술 액션과 터치 버튼은 다음 버전에서 붙인다.
