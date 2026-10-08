SwordMastery PATCH 14 - TOOLTIP REWORK FULL
===========================================

이 패치는 12 + 13 + 이번 툴팁 개편을 합친 누적형 패치입니다.
현재 저장소에 PATCH 14만 덮어쓰면 됩니다.

포함:
- PATCH 12 전투감 / 쿨다운 HUD
- PATCH 13 데미지 공식 / 일섬 99,999 확정치명타
- PATCH 09 DEBUG GMCM
- 최신 UI 아이콘
- PATCH 14 툴팁 전면 개편

툴팁 변경:
- 마우스를 따라다니는 작은 툴팁 폐기
- 스킬에 마우스를 올리면 화면 반대편에 고정형 상세 카드 표시
- 밝은 양피지 배경 + 진한 글씨
- 기술명은 smallFont로 선명하게 표시
- "기초검술 A/B/C" 같은 보조 분류문구 제거
- I / II / III 각 단계:
  설명 + 현재 투자량(0~5) 한 줄 표시
- 현재 단계 / MASTER 상태 표시
- 총 투자 0~15 및 잠금/선택 상태 하단 표시

업로드:
압축을 풀고 아래 파일/폴더를 GitHub 저장소 루트에 그대로 드래그해서 덮어쓰기:
- ModEntry.cs
- ModConfig.cs
- Services/
- UI/
- Integrations/
- assets/icons/

기존 assets/skills.json은 지우지 마세요.
