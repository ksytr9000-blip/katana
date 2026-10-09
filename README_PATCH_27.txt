SwordMastery PATCH 27 - HAUNTED SKULL PROGRESS FIX
=====================================================

PATCH 26 기준 누적 수정입니다.

문제:
귀신들린 해골을 처치해도 OhgiSkullKills가 증가하지 않음.

원인:
기존 판정이 monster.Name == "Bat" + hauntedSkull 내부 플래그를
확인하는 방식이라 실제 1.6.15 개체에서 누락될 수 있었음.
또 NPC 제거 이벤트에서 Health <= 0까지 요구해서 죽음 상태 전환 시
누락될 가능성이 있었음.

수정:
1. 채석장 광산의 귀신들린 해골은 게임 코드상
   new Bat(position, 77377)로 생성됨.
2. 따라서 Bat + mineLevel 77377이면 귀신들린 해골로 확정 판정.
3. location.NameOrUniqueName에 77377이 들어가는 경우도 대응.
4. 기존 hauntedSkull 플래그 / Haunted Skull 이름 / 텍스처 판정은 fallback 유지.
5. e.Removed에서 Health <= 0 조건 제거.
6. 카운트가 오를 때 화면에
   "귀신들린 해골 1/30"
   형태 HUD 메시지 표시.
7. 일지 objective도 즉시 현재/30으로 갱신.

테스트:
- GMCM DEBUG > 오의 퀘스트 시작
- 채석장 광산 귀신들린 해골 1마리 처치
- HUD에 1/30 표시 + 일지에 1/30 반영 확인
