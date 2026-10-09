SwordMastery PATCH 28 - JOURNAL REWARD / NEXUS-VORTEX PREP
================================================================

PATCH 27까지 포함하는 누적형 패치입니다.
현재 저장소에는 PATCH 28만 덮어쓰면 됩니다.

[퀘스트 보상]
- 오의/극의 목표 달성 시 아이템을 즉시 인벤토리에 넣지 않음.
- 퀘스트를 완료 상태로 일지에 유지.
- 일지 하단 보상 상자를 눌러 보상을 수령.
- Vanilla 퀘스트 보상 버튼을 활성화하기 위해 내부적으로 1g 보상 토큰을 사용하고,
  클릭 감지 즉시 1g를 다시 차감하므로 실제 금전 보상은 없음.
- 오의 보상: 오의 비책
- 극의 보상: 깨달음의 물방울
- 인벤토리가 가득 차 있으면 아이템을 버리지 않음.
  보상 지급 대기 상태로 저장하고, 공간이 생기는 즉시 자동 지급.
- 극의 퀘스트의 용의 보주 10개는 목표 완료 시 소모되고 흡수 연출이 발생하며,
  깨달음의 물방울은 일지에서 따로 수령.

[Nexus / Vortex 준비]
- .github/workflows/main.yml 추가/갱신
- GitHub Actions가 버전명을 포함한 Nexus/Vortex용 ZIP 자동 생성:
  SwordMastery-<Version>-Nexus-Vortex.zip
- ZIP 내부는 단일 SwordMastery 폴더 구조
- README_USER.txt
- NEXUS_DESCRIPTION.md
- NEXUS_VORTEX_RELEASE_GUIDE.md
- CHANGELOG.md

[공개 전 TODO]
- manifest Author의 Prototype을 실제 제작자명으로 변경
- 공개용 Version 결정
- Nexus 페이지 생성 후 UpdateKeys에 Nexus Mod ID 추가
