SwordMastery PATCH 11 - IMonitor compile fix

교체 파일:
- Services/CombatService.cs

수정:
- CombatService.cs에 using StardewModdingAPI; 추가
- IMonitor 타입을 찾지 못해 GitHub Actions 빌드가 실패하던 문제 수정

적용:
GitHub에서 Services/CombatService.cs 하나만 덮어쓰기
-> Commit
-> Actions 빌드
