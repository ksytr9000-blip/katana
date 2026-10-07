SwordMastery v0.3 REAL UI + GMCM PATCH
======================================

이 패치는 실제 게임 UI를 아이콘형으로 교체합니다.
이전의 줄형 개발 UI를 완전히 대체합니다.

GitHub 저장소 루트에 그대로 업로드/덮어쓰기:
- ModEntry.cs
- UI/SkillTreeMenu.cs
- Integrations/IGenericModConfigMenuApi.cs
- assets/icons/basic_a.png
- assets/icons/basic_b.png
- assets/icons/basic_c.png
- assets/icons/ohgi_a.png
- assets/icons/ohgi_b.png
- assets/icons/ohgi_c.png
- assets/icons/ultimate_a.png
- assets/icons/ultimate_b.png

주의:
assets 폴더 자체를 없애는 게 아니라 기존 assets 안에 icons 폴더를 추가하면 됩니다.
기존 assets/skills.json은 그대로 남겨두세요.

UI 규칙:
- 기본 스킬 3개: 서로 독립. 화살표 없음.
- 오의 3개: 1개만 선택 가능.
- 오의 선택 후 나머지 2개는 잠금.
- 극의 2개: 1개만 선택 가능.
- 극의 선택 후 나머지 1개는 잠금.
- 기술명은 상시 노출하지 않음.
- 마우스 올렸을 때만 이름/설명/현재 투자/잠금 상태 툴팁 표시.
- 아이콘 클릭:
  기본기 = SP 투자
  오의/극의 미선택 상태 = 분기 선택
  선택된 오의/극의 = SP 투자

GMCM:
- 검술창 열기 키 변경
- 검술 HUD 표시 ON/OFF

빌드:
Commit -> Actions -> 새 Artifact 다운로드 -> SwordMastery 폴더 교체
