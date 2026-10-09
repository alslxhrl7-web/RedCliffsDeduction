# Unity 제작 및 확인 결과

작성일: 2026-09-26

프로젝트: `C:/Users/나민재/Documents/poject/RedCliffsDeduction`

Unity 6000.6.0f1의 실제 Editor에 연결하여 `Assets/Scenes/Investigation.unity`를 수정하고 저장했다. 이번 작업 범위는 문서고의 첫 조사, 조사 장소 선택, 수군 진영으로의 이동과 재방문이다. 이전 구성 문서의 미실행 항목 중 아래 내용은 현재 실제 프로젝트에 반영되어 있다.

## 반영한 흐름

1. 지휘 천막에서 이름 입력과 조사 명령을 마치면 문서고로 이동한다.
2. 빈 문서 보관함 조사와 설평의 형주 경력 진술을 어느 순서로든 확인한다. 두 항목을 끝내면 다음 조사 장소 선택이 열린다.
3. 문서고 입구의 두석에게 경비 상황을 질문할 수 있다. 선택 조사이므로 수군 진영 이동의 필수 조건에는 들어가지 않는다.
4. 수군 진영으로 이동해 채모와 장윤을 어느 순서로든 만난다. 두 천막, 인물, 군기, 길과 울타리를 배치했다.
5. 수첩에서 문서고로 돌아올 수 있다. 같은 실행 중에는 이름과 조사·진술 기록을 유지하며 두 장소를 반복 방문할 수 있다.
6. 수첩을 사실·확인할 일, 설평, 두석, 채모, 장윤의 다섯 페이지로 구성했다. 인물의 말은 미검증 진술로 표시한다.

## 주요 수정 위치

모든 경로는 위 프로젝트 폴더 기준이다.

| 파일 | 반영 내용 |
|---|---|
| `Assets/Scenes/Investigation.unity` | 두석, 수군 진영, 채모·장윤, 두 장소 도착 지점과 이동 참조를 실제 씬에 저장 |
| `Assets/Scripts/Investigation/InvestigationSession.cs` | 조사 완료 조건, 인물 대화, 다섯 페이지 수첩, 장소 선택, 재방문 상태 유지 |
| `Assets/Scripts/Investigation/InvestigationRoute.cs` | 문서고·수군 진영 이동 지점과 현재 장소 관리 |
| `Assets/Scripts/Investigation/FirstPersonInvestigator.cs` | 이동 전환 시 위치·시점·낙하 속도 정리, 창을 닫은 프레임의 입력 중복 방지 |
| `Assets/Scripts/Investigation/IntroductionStage.cs` | 도입부 이동도 같은 위치 전환 처리 사용 |
| `Assets/Scripts/Investigation/InvestigationTarget.cs` | 두석·채모·장윤의 조사 대상과 대화 연결 |
| `Assets/Scripts/Investigation/InvestigationView.cs` | 선택지 수에 맞춰 본문 공간 조정, 네 개 버튼이 겹치지 않게 처리 |
| `Assets/Editor/InvestigationRouteBuilder.cs` | 기존 조사 씬에 배치와 이동 참조를 재연결하는 Editor 메뉴 |
| `Assets/Editor/InvestigationRouteChecks.cs` | 실제 Play 상태에서 조사 순서, 대화 중단, 왕복 이동, 기록, 화면 배치 및 조사 거리 검사 |
| `Assets/Editor/FirstInvestigationChecks.cs` | 기존 검사 메뉴를 확장된 동선 검사로 연결 |
| `README.md` | 현재 흐름, 직접 플레이 순서와 검사 방법 갱신 |

## 확인 결과

- Unity 컴파일 성공.
- 문서 먼저 / 진술 먼저의 두 Play 검사 모두 통과.
- 진술 대화를 중단했을 때 완료로 기록되지 않으며 다시 진행할 수 있음.
- 두석을 만나지 않고도 필수 조사 두 개를 마치면 수군 진영으로 이동 가능.
- 채모·장윤을 서로 다른 순서로 만나도 정상 진행.
- 세 차례 왕복 후 이름과 조사·진술 기록 유지.
- 다섯 조사 대상의 접근 거리와 장애물에 따른 상호작용 검사 통과.
- 실제 Game 화면에서 한글 대사와 선택 버튼을 확인. 자동 검사는 대화 호출 및 조사 판정을 사용했으며 모든 조작을 수동으로 플레이한 검사는 아님.
- 최종 씬 저장 직후 미저장 변경 없음 확인.

## 직접 플레이

`Investigation` 씬에서 Play를 시작한다. WASD 이동, 마우스 시점, E 조사·대화, J 수첩, Esc 창 닫기·일시정지.

빈 보관함과 설평의 형주 경력 진술을 확인한 뒤 **J → 다음 조사 선택 → 수군 진영으로 이동**을 누른다. 수군 진영에서도 수첩의 같은 메뉴로 문서고에 돌아갈 수 있다.

## 현재 범위의 한계

공간과 인물은 임시 도형으로 만든 플레이 가능한 프로토타입이다. 이름과 기록은 이번 실행 동안 유지되며 게임을 종료한 뒤 불러오는 저장 기능은 아직 없다. 이번 구현은 `InvestigationSession`의 상태를 사용한다. 기존 `ClueData/ClueManager`, `SaveManager`, `DialogueCondition`, `EndingManager`의 통합 완료를 뜻하지 않는다.

구체적인 과거 사건의 모순, 경력·호적 재조사, 최종 지목 및 엔딩 연결은 아직 구현하지 않았다. 채모와 장윤의 현재 대사는 훈련 기록과 군량 기록을 구분해야 한다는 조사 방향을 안내한다.

## 확인 화면

- `조사이동_게임화면.png`: 실제 장소 선택 UI
- `문서고_두석대화.png`: 실제 두석 대화 UI
- `수군진영_제작화면.png`: 수군 진영 배치
