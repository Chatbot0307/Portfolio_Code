import random
import getpass
import time

unit1HP = 20
unit2HP = 20

unit1turnSkip = False
unit2turnSkip = False

unit1overCome = False
unit2overCome = False

while(unit1HP > 0 and unit2HP > 0):
  print('''
*********************************************
\t\tDice Duel
*********************************************''')

  unit1Behavior = 0
  unit2Behavior = 0

  print("유닛1 HP : ", unit1HP)
  print("유닛2 HP : ", unit2HP)

  unit1Dice = random.randint(1,6)
  unit2Dice = random.randint(1,6)

  if unit1overCome == True:
    unit1Dice *= 2
    unit1overCome = False
  if unit2overCome == True:
    unit2Dice *= 2
    unit2overCome = False

  print(f"{unit1Dice} vs {unit2Dice}")

  if unit1turnSkip == True:
    print("흐트러짐 상태입니다.")
  else:
    unit1Behavior = int(getpass.getpass("유닛1 행동(숫자로입력) : 1.공격  2.방어  3.반격"))

  if unit2turnSkip == True:
    print("유닛2의 흐트러짐 상태입니다.")
  else:
    unit2Behavior = int(getpass.getpass("유닛2 행동(숫자로입력) : 1.공격  2.방어  3.반격"))

  if unit1turnSkip == True:
    unit1turnSkip = False
    if unit2Behavior == 1:
      unit1HP -= unit2Dice * 2
      print(f"유닛2의 공격, 데미지를 2배로 받는다. 유닛1HP : {unit1HP}")
    elif unit2Behavior == 2:
      unit2HP += unit2Dice
      print(f"유닛2의 회복, 유닛2HP : {unit2HP}")
    elif unit2Behavior == 3:
      print("아무 일도 일어나지 않았다")

  if unit2turnSkip == True:
    unit2turnSkip = False
    if unit1Behavior == 1:
      unit2HP -= unit1Dice * 2
      print(f"유닛1의 공격, 데미지를 2배로 받는다. 유닛2HP : {unit2HP}")
    elif unit1Behavior == 2:
      unit1HP += unit1Dice
      print(f"유닛1의 회복, 유닛1HP : {unit1HP}")
    elif unit1Behavior == 3:
      print("아무 일도 일어나지 않았다")

  if unit1Behavior == 1 and unit2Behavior == 1:
    if unit1Dice > unit2Dice:
      unit2HP -= unit1Dice
      print(f"유닛1의 공격, 유닛2HP : {unit2HP}")
    elif unit1Dice < unit2Dice:
      unit1HP -= unit2Dice
      print(f"유닛2의 공격, 유닛1HP : {unit1HP}")
    else:
      print("아무 일도 일어나지 않았다")

  elif unit1Behavior == 2 and unit2Behavior == 2:
    print("아무 일도 일어나지 않았다")

  elif unit1Behavior == 3 and unit2Behavior == 3:
    print("아무 일도 일어나지 않았다")

  elif unit1Behavior == 1 and unit2Behavior == 2:
    if unit1Dice > unit2Dice:
      unit2HP -= unit1Dice - unit2Dice
      print(f"유닛2의 방어, 유닛2HP : {unit2HP}")
    elif unit1Dice < unit2Dice:
      unit1overCome = True
      print("유닛1의 극복, 다음턴 주사위 x 2")

  elif unit1Behavior == 2 and unit2Behavior == 1:
    if unit1Dice < unit2Dice:
      unit1HP -= unit2Dice - unit1Dice
      print(f"유닛1의 방어, 유닛1HP : {unit1HP}")
    elif unit1Dice > unit2Dice:
      unit2overCome = True
      print("유닛2의 극복, 다음턴 주사위 x 2")

  elif unit1Behavior == 1 and unit2Behavior == 3:
    if unit1Dice > unit2Dice:
      unit1HP -= unit1Dice + unit2Dice
      print(f"유닛2의 반격, 유닛1HP : {unit1HP}")
    else:
      print("아무 일도 일어나지 않았다")

  elif unit1Behavior == 3 and unit2Behavior == 1:
    if unit1Dice < unit2Dice:
      unit2HP -= unit1Dice + unit2Dice
      print(f"유닛1의 반격, 유닛2HP : {unit2HP}")
    else:
      print("아무 일도 일어나지 않았다")

  elif unit1Behavior == 2 and unit2Behavior == 3:
    if unit1Dice > unit2Dice:
      print("유닛2의 흐트러짐")
      unit2turnSkip = True
    else:
      print("아무 일도 일어나지 않았다")

  elif unit1Behavior == 3 and unit2Behavior == 2:
    if unit1Dice < unit2Dice:
      print("유닛1의 흐트러짐")
      unit1turnSkip = True
    else:
      print("아무 일도 일어나지 않았다")

  time.sleep(4)

print('''
*********************************************
\t\tDice Duel
*********************************************''')

if unit1HP <= 0:
  print("유닛2의 승리!")
if unit2HP <= 0:
  print("유닛1의 승리!")