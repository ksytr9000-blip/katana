SwordMastery PATCH 26 - HAUNTED SKULL / ARMORED BUG FIX
==========================================================

PATCH 25까지 모두 포함하는 누적형 패치입니다.
현재 저장소에는 PATCH 26만 덮어쓰면 됩니다.

[1] 오의 해방 퀘스트 대상 표기 수정
기존:
- 어둠의 해골 30마리

변경:
- 귀신들린 해골(채석장 광산) 30마리

실제 처치 판정은 Haunted Skull을 계속 사용합니다.
Haunted Skull은 Stardew 내부에서 Bat 변종으로 구현되어 있어
기존의 hauntedSkull 판별 로직을 그대로 유지합니다.

[2] 검술의 극 vs 갑충(Armored Bug)
원인:
- Armored Bug는 내부 isArmoredBug 플래그가 켜져 있으면
  현재 들고 있는 무기가 Bug Killer인지 검사함
- 폭발(isBomb=true) 피해도 오히려 거부함

수정:
- 검술의 극이 Armored Bug를 감지하면
  내부 armor 플래그를 타격 순간에만 임시 OFF
- 99,999 확정 치명타를 일반 hit로 적용
- 도구/무기/맨손 여부와 무관하게 처치 가능
- 만약 살아남으면 armor 플래그를 다시 복구
- 필드명 차이를 대비해 reflection으로
  isArmoredBug / IsArmoredBug / isArmored / IsArmored를 모두 대응

미라와 바위게 특수처치 로직은 그대로 유지됩니다.
