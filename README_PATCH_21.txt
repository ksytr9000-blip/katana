SwordMastery PATCH 21 - QUEST JOURNAL FIX
============================================

PATCH 20까지 모두 포함하는 누적형 패치입니다.
현재 저장소에는 PATCH 21만 덮어쓰면 됩니다.

수정:
1. 퀘스트 일지 등록 방식 변경
- 기존: reflection으로 Farmer.addQuest(string) 탐색
- 변경: Stardew Valley 1.6의 Game1.player.addQuest(string) 직접 호출

2. Data/Quests 캐시 강제 갱신
- 세이브 로드 시 Data/Quests 캐시를 무효화
- 퀘스트를 넣기 직전에도 다시 로드해 커스텀 퀘스트 ID 존재 여부 확인

3. 등록 성공 여부 검증
- addQuest 호출 후 실제 questLog에 퀘스트가 들어갔는지 다시 확인

4. fallback 추가
- 직접 addQuest가 실패하는 특이 환경에서는
  Quest.getQuestFromId(string)을 reflection으로 호출해 questLog에 직접 추가 시도

5. DEBUG 퀘스트 버튼
- 성공: "일지에 추가했습니다."
- 실패: 화면에 DEBUG 오류 메시지 표시 + SMAPI 로그에 상세 오류 남김

테스트:
GMCM > DEBUG 테스트 도구에서
- 오의 퀘스트 시작
- 극의 퀘스트 시작
버튼을 눌러 일지에 즉시 들어오는지 확인.
