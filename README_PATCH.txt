SwordMastery UI + Tooltip + HUD/GMCM 통합 패치
==============================================

GitHub에 그대로 덮어쓸 파일:
- ModEntry.cs
- ModConfig.cs
- UI/SkillTreeMenu.cs
- Integrations/IGenericModConfigMenuApi.cs

이번 패치:
1. 스킬창 전체 크기 확대
   - 최대 1280 x 880
   - 아이콘도 더 크게
   - 기본/오의/극의 블록 간격 확대

2. I / II / III 단계 표시 간격 확대
   - 30px 간격으로 변경
   - 아이콘 아래 공간도 확대

3. 기본 스킬 / 오의 / 극의 제목 박스 확대
   - 글자가 프레임 밖으로 튀어나오는 현상 완화

4. EXP 헤더 정리
   - EXP 글자와 바가 겹치지 않도록 바를 오른쪽으로 이동
   - 바 길이는 최대 255px로 축소

5. 툴팁 디자인 변경
   - 더 큰 470px 패널
   - 어두운 갈색 내부 + 금색 제목
   - 아이콘 프레임
   - 설명/현재 투자/상태 영역 분리

6. GMCM HUD 위치 설정 추가
   - 검술 HUD 표시 ON/OFF
   - HUD X 위치
   - HUD Y 위치
   - 검술창 열기 키

기본 HUD 위치:
X = 24
Y = 24

기존 config.json에 HudX/HudY가 없어도 기본값으로 자동 적용됩니다.
