# 적벽추리 (Red Cliffs Deduction)

Unity 6000.6.0f1 · URP · 한국어 1인칭 추리 게임. 적벽대전 직전 조조군에서 바뀐 군령(제17호)을 조사해 조조에게 최종 보고한다.

- 작업 폴더: 이 폴더(`Documents\poject\RedCliffsDeduction`)만 쓴다. `Documents\RedCliffsDeduction`은 예전 사본이므로 건드리지 않는다.
- 메인 씬: `Assets/Scenes/Investigation.unity`
- 공개 빌드: 0.1.0-demo.7 (itch.io). 현재 작업 중: 0.1.0-demo.8 (`Docs/PATCH_NOTES.md` 맨 위 항목)
- 사용자에게는 한국어로, 짧고 쉬운 말로 보고한다.

## 검사·최적화 요청을 받으면

- "검사해" → `Tools/ClaudeCode/commands/check.md`의 절차를 따른다.
- "최적화해" → `Tools/ClaudeCode/commands/optimize.md`의 절차를 따른다.
- `.claude/commands`에 같은 파일이 없으면, 처음 한 번 `Tools\ClaudeCode\install.cmd`를 실행해 `/check`, `/optimize` 명령과 권한 설정(`.claude/settings.json`: commit·push 전에 묻기, `Library`·예전 사본 수정 금지)을 설치한다. 이미 `.claude/settings.json`이 있으면 덮어쓰지 않는다.

## 꼭 지킬 것

1. **있는 것을 지우거나 다시 만들지 않는다.** 사용자가 만든 씬 배치, 저장 파일, 모델, 소리, 연출, 2D 증거 문서, 사무 소품, 연결 길, 발소리, 소리 설정을 보존한다. 씬을 새로 생성·덮어쓰는 메뉴는 실행하지 않는다(아래 "실행 전에 물어볼 메뉴").
2. **저장 호환:** 군령 조사 저장은 버전 2, 파일은 `order-investigation-save.json`으로 따로 둔다. 예전 저장이 계속 읽혀야 한다.
3. **한진**은 아직 기존 서기 외형을 임시로 쓴다. 완성됐다고 쓰지 않는다.
4. **확인하지 않은 것은 완료라고 하지 않는다.** 보고할 때 다음 네 가지를 항상 따로 적는다: 구현 / 검사(무엇을 어떻게 돌렸는지) / 빌드 / GitHub 업로드.
5. **git commit, push, GitHub 업로드는 사용자가 요청할 때만** 한다. 토큰이나 비밀번호를 파일에 쓰지 않는다.
6. **기록 위치를 구분한다.**
   - 발견한 문제와 그 문제를 어떻게 고쳤는지 → `Docs/수정기록.md` (항목마다 발견·상태·수정 내용·검증)
   - 버전 단위 변경 요약 → `Docs/PATCH_NOTES.md` (버전·날짜, 추가/개선/수정/검증/남은 문제 구분, 빌드·업로드 상태 별도)
7. **플레이테스트 기록(`PlaytestLogs/`)은 개인정보 없이 유지한다.** 이름, 입력 글자, 화면 본문, 컴퓨터 정보를 기록하는 코드를 넣지 않는다. 기록 파일 내용을 다른 곳으로 보내지 않는다.

## 게임 규칙 (바꾸려면 먼저 물어본다)

- 최종 보고: 결론(지목) → 제출 근거 → 시행 조치. 엔딩은 **실제로 제출한 근거**로만 판정한다(`OrderFinalReport.Resolve`).
- TRUE: 한진 지목 + 근거 6개 정확히(값 63, 미끼 근거 제외) + 조치 1(제17호·대체 재발행 중지와 강 입구 배치 복원).
- BAD: 다른 인물 지목, 한진을 지목했지만 제출·고의 근거 없음, 변경 근거 없는 보고.
- NORMAL: 그 밖의 부분 입증.
- 중간 보고는 필수 조건이 아니다. 엔딩 문구는 제출 내용과 맞아야 한다.
- 미끼 근거 "문서실 출입 기록과 한진의 취급 권한"(비트 64)을 함께 내면 TRUE가 되지 않는다.
- 수첩 세 질문(변경 시점 → 제출자 → 고의)은 각각 정답 1개와 그럴듯한 오답 2개를 항상 함께 보여 준다. 증거가 모이기 전에는 세 선택지가 모두 같은 중립 답을 준다. 정답이 티 나게 바꾸지 않는다.

## 검사 방법

### 1. 컴파일 검사 (Unity를 열지 않아도 됨)

```
Tools\CompileCheck\compile-check.cmd
```

- 게임 빌드용(UNITY_EDITOR 없음), 에디터용 런타임, 에디터·검사 코드를 각각 컴파일한다. 마지막 줄이 `COMPILE_CHECK_PASS`면 통과.
- 설치된 Unity 엔진 파일(`C:\Program Files\Unity\Hub\Editor\6000.6.0f1\Editor\Data\Managed`)과 `Library\ScriptAssemblies`의 패키지 어셈블리를 참조한다. Unity를 다른 곳에 설치했다면 `set UNITY_MANAGED=...\Editor\Data\Managed`.
- `Library\ScriptAssemblies`는 Unity가 한 번 열려 컴파일한 뒤에 생긴다. 패키지를 바꿨다면 Unity를 한 번 열어 갱신한다.
- 출력은 `Temp\CompileCheck`에 생긴다. 게임에는 포함되지 않는다.
- 컴파일 통과는 실행 결과까지 보장하지 않는다. 아래 플레이 모드 검사를 같이 돌린다.

### 2. Unity 플레이 모드 검사 (Unity를 연 상태)

Unity MCP가 연결돼 있으면 메뉴 실행·플레이 모드 전환·콘솔 확인에 쓴다. 연결이 안 돼 있으면 사용자에게 메뉴를 눌러 달라고 하고 결과 파일을 읽는다.

| 검사 | 메뉴 | 시작 상태 | 결과 |
|---|---|---|---|
| 군령 조사 전체 (진행·저장·세 질문·최종 보고·엔딩·플레이테스트 기록) | `적벽추리/군령 조사/진행과 저장 검사` | 플레이 모드에서 타이틀 화면이 보일 때 | `Logs/OrderChapter/checks.txt` 마지막 줄 `COMPLETE failures=0` |
| 타이틀 화면 | `적벽추리/타이틀/화면과 버튼 확인`, `시작 버튼 확인`, `종료 버튼 확인` | 플레이 모드 | `Logs/TitleScreen/check.txt` |
| 엔딩 조건 | `적벽추리/엔딩 조건 확인 (실행 없음)` | 편집 모드 | 콘솔 |

- 군령 조사 검사는 실패가 나오면 그 자리에서 멈춘다. `checks.txt`의 `FAIL` 줄과 콘솔 오류를 함께 본다. 검사는 임시 저장 폴더를 쓰고, 끝나면 검사용 세션을 꺼서 실제 저장 슬롯을 건드리지 않는다.
- `적벽추리/플레이 검증/*` 일부는 예전 사건(화공대비책·설평) 흐름용이다. 실패하면 바로 고치지 말고 현재 군령 조사와 관련 있는지 먼저 확인하고 사용자에게 알린다.
- 예전에 Unity MCP를 쓸 때 겪은 점:
  - 컴파일 직후에는 "Unity not detected"가 날 수 있으니 잠시 기다렸다 다시 시도한다.
  - 메뉴 실행과 다른 코드를 한 번에 섞으면 "User interactions are not supported"가 날 수 있으니 따로 호출한다.
  - 코드 실행 명령은 `internal class CommandScript : IRunCommand` 형태여야 한다.
  - 플레이 모드가 필요한 검사는 먼저 플레이 모드에 들어간다.

### 3. 실행 전에 물어볼 메뉴 (씬·에셋·빌드를 바꿈)

이름에 **적용, 추가, 생성, 제작, 수정, 연결, 다듬기, 재가져오기, 초기화, 빌드, zip, 종료**가 들어간 메뉴는 씬이나 에셋을 바꾸거나 빌드를 만든다. 사용자에게 먼저 묻는다. 예:

- `적벽추리/첫 조사 장면 생성 또는 열기`
- `적벽추리/문서고 소품 제작과 적용`
- `적벽추리/바르코 인물 모델 적용`
- `적벽추리/데모 수정/수정 데모 빌드`
- `적벽추리/데모 수정/배포 zip 만들기`
- `적벽추리/렌더링 검증/저장 후 편집기 종료`

## 최적화 원칙

1. **먼저 잰다.** 바꾸기 전에 기준값을 남긴다(프레임 시간·GC 할당·드로우콜·그림자 수, 빌드 크기, 로딩 시간 중 해당하는 것). 잴 수 없으면 추측이라고 밝힌다.
2. **보이는 결과와 플레이를 바꾸지 않는다.** 화질, 조명, 그림자, 배치, 대사, 판정이 달라지는 변경은 전후 화면이나 수치를 보여 주고 사용자 승인을 받는다.
3. **한 번에 한 가지씩** 바꾸고, 바꿀 때마다 컴파일 검사 + 군령 조사 검사를 다시 돌린다.
4. 저장 형식, 메뉴 이름, 검사가 찾는 버튼 문구(`view.AddChoice` 라벨)는 바꾸지 않는다. 바꿔야 하면 검사 코드도 같이 고친다.
5. 결과를 `Docs/PATCH_NOTES.md`의 작업 중 버전에 "개선"으로 적는다. 측정값 전·후를 함께 적는다.

이미 있는 도구 (Python + Pillow 필요):

- `Tools/optimize_repository_pngs.py`: `Assets/Art`의 PNG를 픽셀이 같을 때만 무손실 재압축한다. 저장소 용량용이며 빌드 크기는 Unity 텍스처 가져오기 설정이 정한다.
- `Tools/verify_repository_assets.py`: 바뀐 PNG의 픽셀이 HEAD와 같은지, 지운 에셋의 GUID를 아무 곳에서도 쓰지 않는지 확인한다(git 필요). PNG를 손대면 반드시 함께 돌린다.

살펴볼 후보 (아직 측정 안 함, 사실로 단정하지 말 것):

- 매 프레임 실행되는 `Update`(`InvestigationSession`, `FirstPersonInvestigator`, `InvestigationView.LateUpdate`, `VarcoIdleMotion`, `DistanceFootsteps`)의 할당과 문자열 생성
- 등불 그림자 예산(`적벽추리/등불 그림자 예산 조정`이 있음): 그림자 드리우는 광원 수
- 텍스처·오디오 가져오기 설정과 빌드 크기 (`Builds/` 빌드 보고서 참고)
- 대화 화면을 열 때마다 버튼 오브젝트를 새로 만들고 지우는 구조(`InvestigationView.ShowModal`/`AddChoice`). 체감 문제가 측정될 때만 손댄다.

## 주요 파일

- `Assets/Scripts/Investigation/InvestigationSession*.cs`: 게임 진행(partial 클래스). 군령 조사는 `.Orders.cs`, `.OrderFollowUp.cs`(세 질문), `.OrderFinal.cs`(최종 보고·엔딩)
- `OrderFinalReport.cs`: 엔딩 판정 / `OrderInvestigationState.cs`: 조사 사실·선행 조건
- `InvestigationView.cs`: 화면·선택지 UI
- `PlaytestRecorder.cs`·`PlaytestObserver.cs`·`PlaytestObserverRunner.cs`: 숨은 플레이테스트 기록. `playtest.on` 파일이 있을 때만 동작
- `Assets/Editor/OrderChapterChecks.cs`(+ `OrderFollowUpChecks`, `OrderFinalChecks`, `PlaytestObserverChecks`): 군령 조사 검사
- `Docs/`: 기획, 정답지, 수정기록, 패치노트
