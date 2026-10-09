# Sword Mastery - 검술 마스터리

Stardew Valley에 독립적인 검술 성장 시스템을 추가하는 SMAPI 모드입니다.

## 주요 기능
- 몬스터 처치 기반 검술 레벨 / 경험치 / SP
- 기초검술 3계통과 단계별 강화
- 오의 3계통 중 1개 선택
- 극의 2계통 중 1개 선택
- 액티브 검술, 다단 참격, 검기, 이동기, 패시브 검술
- 검술 전용 HUD 및 쿨다운 HUD
- 오의/극의 해방 퀘스트
- 오의 비책, 깨달음의 물방울, 용의 보주 커스텀 아이템
- GMCM 연동

## Requirements
**Required**
- Stardew Valley 1.6.x
- SMAPI 4.3.0+

**Optional**
- Generic Mod Config Menu (GMCM)

## Installation

### Vortex
Nexus Mods에서 **Mod Manager Download**를 누르고 Vortex에서 설치/활성화하세요.
배포 ZIP은 Vortex가 Stardew Valley의 `Mods` 폴더에 배치할 수 있도록
`SwordMastery/manifest.json` 구조로 패키징되어 있습니다.

### Manual
압축을 풀고 `SwordMastery` 폴더를 Stardew Valley의 `Mods` 폴더 안에 넣으세요.

## Compatibility
- PC / Stardew Valley 1.6.x / SMAPI
- GMCM은 선택 사항입니다.
- Android는 별도 호환 테스트가 완료되기 전까지 실험적 지원입니다.

## Configuration
GMCM 설치 시 게임 내 모드 설정 메뉴에서:
- 스킬 단축키
- 검술 HUD 표시/위치
- 쿨다운 HUD 표시/위치
- 개발용 DEBUG 기능
을 조정할 수 있습니다.

## Update note
Nexus 페이지 생성 후 `manifest.json`에 Nexus Mod ID를 UpdateKeys로 추가하면
SMAPI의 업데이트 알림과 연결할 수 있습니다.
예:
`"UpdateKeys": ["Nexus:12345"]`
