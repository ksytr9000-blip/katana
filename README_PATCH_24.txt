SwordMastery PATCH 24 - ARMORED BUG BUILD FIX
================================================

PATCH 23까지 모두 포함하는 누적형 패치입니다.
현재 저장소에는 PATCH 24만 덮어쓰면 됩니다.

빌드 오류 수정:

1. ArmoredBug 타입 오류
- Stardew Valley 1.6에는 ArmoredBug라는 별도 C# 클래스가 없음
- 갑충은 Bug 타입의 변종이므로
  monster is Bug + monster.Name == "Armored Bug"
  방식으로 판별하도록 수정

2. monsterBox 중복 선언 오류
- 바깥 스코프에서 이미 사용 중인 monsterBox와 충돌하지 않도록
  specialMonsterBox로 변수명 변경

검술의 극 특수 처치 대상은 그대로:
- 미라
- 갑충(Armored Bug)
- 바위게(Rock Crab 계열)

PATCH 22의 퀘스트 포맷 수정도 그대로 포함됨.
