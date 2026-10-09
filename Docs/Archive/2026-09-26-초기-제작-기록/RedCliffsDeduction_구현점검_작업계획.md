# RedCliffsDeduction 구현 현황 및 후속 작업

점검일: 2026-09-26
프로젝트: C:/Users/나민재/Documents/poject/RedCliffsDeduction

## 후속 반영 — 스테이지 선택 기본 화면

2026-09-26 후속 요청으로 실제 프로젝트에 첫 조사 완료 → 수첩의 ‘다음 조사 선택’ → 선택 화면 → 수첩/문서고 복귀를 구현했다. 채모·장윤 조사는 준비 중인 비활성 항목이다. 같은 씬에서 이름과 조사 기록을 유지하며, 창을 닫은 프레임의 중복 입력도 막았다.

런타임 및 에디터 코드 컴파일은 통과했다. 실제 Play 검사와 화면 확인은 실행 중인 Unity의 연결 파일 접근 제한으로 미실행이다. 조사 순서 두 가지 및 선택 화면 반복 진입을 다루는 실행 검사는 프로젝트에 추가했다.

아래 본문은 구현 전 점검과 작업 계획의 기록이다. 변경 파일과 현재 상태, 첫 조사 장소 제안은 같은 출력 폴더의 ‘스테이지선택_반영결과와_첫조사구성.md’에 정리했다.

## 결론과 점검 범위

현재 프로젝트는 **조조 알현 → 이름 입력 → 조사 명령 → 문서고 진입 → 빈 보관함 조사·설평 진술 확보 → 수첩 확인**까지 구현된 첫 조사 프로토타입이다. 스테이지 선택, 범용 단서·저장·대화 조건·엔딩 관리자는 아직 반영되어 있지 않다.

README도 기존 프로젝트의 코드·애셋이 아직 이식되지 않았다고 명시한다. 따라서 예전에 논의한 관리자 구현이 이 폴더에 존재한다고 전제하면 안 된다. 다른 프로젝트나 과거 버전에 있는지는 이번 점검으로 판단할 수 없다.

참조된 ChatGPT 대화에서 조회 가능한 내용은 “RedCliffsDeduction 여기 폴더 열어서 마저 할부분 적어봐” 한 문장뿐이었다. 엔딩 규칙은 로컬 Docs/사건 정답지.md를 근거로 삼았다. 후속 요청에 따라 현재 검토 초점은 스테이지 선택 전환이다.

Assets의 사용자 코드, 씬 직렬화 데이터, 설정 및 문서를 정적으로 점검했다. Unity Play 실행·새 빌드·테스트 실행은 하지 않았다. 프로젝트 파일은 수정하지 않았다.

## 구조

- Unity 6000.6.0f1, URP, 새 Input System, uGUI.
- Assets/Scripts/Investigation: 게임 실행 코드 5개. InvestigationSession, InvestigationView, InvestigationTarget, IntroductionStage, FirstPersonInvestigator.
- Assets/Editor: 장면 제작 도구 2개와 실행 검사 도구 1개.
- Assets/Scenes: Investigation.unity, SampleScene.unity. 빌드 설정에는 Investigation만 활성 등록.
- Assets/Art/PrototypeMaterials: 임시 공간·인물용 재질.
- Assets/Settings: 렌더링 설정. TutorialInfo와 Readme는 템플릿 안내용.
- Docs/사건 정답지.md: 범인·진술 비교·핵심 증거·엔딩 기획.
- Library, Temp, Logs: 생성 파일. 기능 구현을 수정할 주 대상이 아니다.

## 기능별 반영 상태

| 항목 | 상태 | 실제 근거와 남은 부분 |
|---|---|---|
| 도입부 및 문서고 이동 | 구현 및 씬 참조 확인 | IntroductionStage.cs:12–30. 같은 씬에서 위치를 옮기고 천막을 비활성화한다. |
| 스테이지 선택 전환 | 미구현 | StageSelect 씬·관리자·선택 UI·런타임 씬 로딩 없음. IntroductionStage의 이름이 스테이지 관리 기능을 의미하지 않는다. |
| 첫 조사 완료 | 부분 구현 | InvestigationSession.cs:19의 두 bool AND 판정. :181, :186–191에서 안내만 변경. 완료 이벤트·다음 조사 호출 없음. |
| ClueData / ClueManager | 미구현 | 클래스와 단서 데이터 애셋 없음. 문서 조사 상태는 DocumentInspected bool 하나로 관리. |
| 단서·진술 구분 | 제한적으로 구현 | 수첩에서 문서 부재는 확인된 사실, 설평 경력은 미검증 진술로 표시. 범용 증거 목록이나 비교 판정은 없음. |
| SaveManager 및 플래그 | 미구현 | 메모리 내 bool과 이름만 존재. 저장·불러오기·회차별 초기화 없음. |
| DialogueCondition | 미구현 | DocumentInspected에 따른 첫 인사 분기만 존재. 조건 데이터·평가기·선택지 해금 시스템 없음. |
| EndingManager | 미구현 | 최종 지목, 엔딩 판정, 엔딩 화면, 해금 기록 없음. |
| 핵심 증거와 엔딩 연결 | 문서에만 존재 | 호적·밀서 단서 ID는 사건 정답지에만 등장. 획득 위치·데이터·판정 코드 없음. |
| 3일 조사 기한 | 대사에만 존재 | 조조가 사흘을 주지만 날짜·행동 소모·시간 초과 처리는 없음. |

### 현재 코드에서 특히 주의할 점

1. InvestigationSession.cs:10의 MissingDocumentClueId와 :11의 TestimonyFlagId는 선언만 있고, 저장소나 단서 등록 API에서 사용하지 않는다. ID가 있다는 사실만으로 단서·플래그 시스템 구현으로 볼 수 없다.
2. InspectDocument(:116)는 즉시 DocumentInspected를 true로 바꾼다. 설평 진술(:133–140)은 대사를 끝까지 확인한 뒤 TestimonyHeard가 true가 된다. 대화 취소 시 미획득 처리하는 현재 의미는 유지할 가치가 있다.
3. FirstInvestigationComplete는 게임 전체 클리어가 아니라 첫 두 행동의 완료 상태다. 이를 곧바로 엔딩 조건으로 재사용하면 범위가 섞인다.
4. 일시정지는 이동과 입력을 막는 방식이다. 향후 시간 시스템을 넣을 때 메뉴를 연 상태에서도 조사 시간이 흐를지 별도 연결이 필요하다.

## 실제 씬 연결

Investigation.unity의 다음 위치에서 직렬화 참조가 존재함을 확인했다.

- :1382–1383 — InvestigationTarget, kind=1(설평).
- :4517–4519 — InvestigationSession에서 view와 introductionStage 참조.
- :7771–7775 — IntroductionStage에서 player, tentSpawn, archiveSpawn, tent 참조.
- :11954–11955 — InvestigationTarget, kind=0(빈 문서 보관함).
- :15524–15529 — FirstPersonInvestigator의 카메라·세션과 상호작용 거리 3.
- :16608–16618 — InvestigationView의 목표·본문·선택지 등 UI 참조. font의 초기 값은 0이지만 Awake에서 OS 폰트를 생성하므로 이것만으로 누락 오류는 아니다.

## 파일별 수정 위치와 우선순위

아래 기존 위치는 현재 파일의 줄 번호다. “신규 제안” 파일은 아직 존재하지 않는다. 모든 상대 경로는 위 프로젝트 루트 기준이다.

### P0 — 상태·저장·단서의 공통 기반

**기존 수정: Assets/Scripts/Investigation/InvestigationSession.cs**

- :10–19 — 단서 ID, 진술 플래그, 이름, 완료 상태를 공통 진행 데이터와 연결. bool과 중앙 저장 상태를 따로 관리해 값이 어긋나지 않도록 한다.
- :30–39 — 새 게임과 이어하기를 구분. 저장을 복원한 뒤 이미 완료한 이름 입력·서막의 재실행 여부 결정.
- :116–123 — 문서 조사 시 ClueManager를 통해 clue_document_missing을 중복 없이 획득.
- :133–140 — 대사 완료 시 진술 플래그 설정. 취소하면 등록되지 않는 현재 동작 유지.
- :172–191 — 수첩과 목표를 공통 단서·진행 데이터에서 생성.

**신규 제안**

- Assets/Scripts/Save/SaveData.cs: 버전, 이름, 진행 위치, 조사 시간, 단서 ID, 진술/진행 플래그.
- Assets/Scripts/Save/SaveManager.cs: 저장·복원·새 게임 초기화. 영구 해금과 현재 사건/스테이지 상태의 수명 구분.
- Assets/Scripts/Clues/ClueData.cs: 고유 ID, 표시 이름, 설명, 사실/미검증 진술 유형.
- Assets/Scripts/Clues/ClueManager.cs: ID 기반 획득·조회·중복 방지·저장 복원·변경 통지.
- Assets/Data/Clues/: 문서 부재, 호적 기록 부재, 본명 밀서의 실제 데이터 애셋.

완료 기준: 반복 조사로 중복 등록되지 않고, 재실행 후 기록이 복원되며, 새 게임은 사건 진행 상태를 명시한 범위대로 지운다.

### 우선 검토 — 첫 조사에서 스테이지 선택으로 전환

**현재 흐름과 제안 흐름**

- 현재: 문서 보관함 조사 + 설평 경력 진술 확인 → ‘첫 조사 완료’ 안내 → 문서고에 계속 머무름.
- 제안: 두 조사 완료 → 수첩에서 ‘다음 조사 선택’ → 스테이지 선택 화면 → 구현된 다음 조사로 이동.
- 선택 화면에서는 문서고 복귀가 가능하고, 이름·획득 단서·확인한 진술을 유지한다.
- 채모·장윤은 현재 후속 조사 대상으로 안내만 되어 있다. 둘을 별도 스테이지로 나눌지 하나로 묶을지는 확정하지 않았으며, 실제 조사 장면도 아직 없다. 목적지가 구현되기 전에는 이동 가능 상태로 표시하지 않는다.

**전환 시점에서 주의할 점**

- UpdateObjective(:186)에는 목표 문구 갱신만 유지한다. InspectDocument는 :119에서 이 함수를 부른 뒤 :120에서 조사 결과 창을 띄우며, 설평 진술 완료도 :135 호출 뒤 :136에서 진술 창을 띄운다. UpdateObjective 안에서 선택 창을 열면 뒤의 결과 창이 바로 덮어쓴다.
- ShowJournal(:172–183)에서 FirstInvestigationComplete일 때만 ‘다음 조사 선택’ 버튼을 추가하는 것이 현재 구조에서 명확한 진입점이다. 수첩을 닫고 조사를 계속하는 선택지도 유지한다.
- 선택 화면을 여는 함수에서도 완료 조건을 다시 검사하고, 이동 중 중복 클릭을 막는다.
- 대사를 읽던 중 취소한 경우에는 TestimonyHeard가 false이므로 다음 단계가 열리지 않아야 한다.

**첫 구현 범위 제안: 같은 씬의 선택 패널**

현재 InvestigationSession과 화면을 유지한 채 선택 패널을 열면, 같은 실행 중 이름과 두 조사 상태를 그대로 보존할 수 있다. 재실행 후 복원은 별도의 저장 기능이 필요하다. 후속 조사 씬이 생길 때 씬 전환과 공통 진행 데이터까지 확장한다.

**기존 파일의 수정 위치**

| 파일 및 현재 줄 | 수정 내용 |
|---|---|
| Assets/Scripts/Investigation/InvestigationSession.cs:172–183 | 완료 조건에 따라 수첩에 ‘다음 조사 선택’ 버튼 추가. 선택 화면 진입 함수 연결. |
| Assets/Scripts/Investigation/InvestigationSession.cs:84–105 | 선택 패널에서 이동 잠금·커서 표시·닫기 동작을 일관되게 관리. 기존 대화 콜백이 남지 않도록 정리. |
| Assets/Scripts/Investigation/InvestigationSession.cs:186–191 | 완료 안내를 ‘J키로 수첩에서 다음 조사 선택’처럼 실제 행동에 맞춰 변경. |
| Assets/Scripts/Investigation/InvestigationView.cs:49–89 | 선택 화면과 버튼을 표시하고, 준비되지 않은 목적지는 실행할 수 없게 구분. |
| Assets/Scripts/Investigation/IntroductionStage.cs:18–30 | 천막 → 문서고의 기존 공간 이동 역할 유지. 후속 목적지 이동은 별도로 연결. |

**후속 조사 씬을 실제로 추가할 때 필요한 작업**

- 신규 제안 Assets/Scripts/Stages/StageSelectController.cs: 선택 목록, 진행 잠금, 목적지 준비 상태, 진입 요청 처리.
- 신규 제안 Assets/Scripts/Stages/StageFlowManager.cs: 현재 단계, 해금·완료 상태, 중복 전환 방지, 목적지 로딩 담당.
- 씬 밖에서 유지되는 공통 진행 데이터에 이름·단서·진술 상태를 이전. 현재 상태에서 Investigation 씬을 다시 로드하면 새 세션이 시작되고 Start(:30)가 이름 입력·서막을 다시 띄운다.
- 별도 StageSelect.unity를 사용하기로 정할 경우 ProjectSettings/EditorBuildSettings.asset에 등록. 같은 씬의 패널 방식에는 이 씬이 필요하지 않다.
- Assets/Editor/FirstInvestigationBuilder.cs:104가 빌드 씬 목록을 하나로 덮어쓰므로, 다른 씬을 추가할 때 기존 목록을 보존하도록 변경.

완료 기준: 조사 순서와 무관하게 둘 다 완료했을 때 진입 가능, 결과 대사 도중 강제 전환 없음, 선택 취소 후 이동 정상 복구, 이름·조사 기록 유지, 미구현 목적지 실행 차단.

### P1 — 진술 비교와 DialogueCondition

**기존 수정**

- InvestigationSession.cs:125–140 — 하드코딩된 대화를 조건·효과 데이터 기반으로 이전.
- InvestigationTarget.cs:7–15 — 현재 대상은 두 종류뿐이다. 채모·장윤과 추가 조사 대상의 상호작용을 확장.
- InvestigationView.cs:67–89 — 조건을 통과한 선택지를 표시. UI 자체에 증거 획득이나 엔딩 규칙을 중복 구현하지 않는다.
- Docs/사건 정답지.md:30–36, :47–52 — 진술 비교의 구체적인 장소·시기·대사는 아직 미확정. 예시를 확정 설정으로 취급하지 않는다.

**신규 제안**

- Assets/Scripts/Dialogue/DialogueCondition.cs: 단서 보유, 플래그, 조사 시점, AND/OR 조건 평가.
- 대사·선택지 데이터 및 완료 효과 처리: 진술 획득, 조사 경로 해금, 시간 소모.
- 채모·장윤 진술, 비교 판정, 경력 서류 재조사, 호적 조사 콘텐츠.

완료 기준: 조사 순서가 달라도 진술이 유지되며, 모순 발견으로 재조사 경로가 열리고, 모순을 놓쳤다고 즉시 실패하지 않는다.

### P1 — 최종 지목과 EndingManager

**신규 제안**

- Assets/Scripts/Endings/EndingManager.cs: 지목 대상과 획득 증거를 받아 결과 판정.
- Assets/Scripts/Endings/AccusationController.cs: 최종 지목과 확정 처리.
- Assets/Scripts/Progression/InvestigationClock.cs: 조사 기한과 행동/시간 소모 관리.
- 엔딩 결과 표시, 엔딩 해금 저장, 재시작/선택 복귀 UI.

**기존 연결 위치**

- InvestigationSession.cs:172 — 증거 검토 UI에서 최종 지목 흐름으로 연결할 진입점 검토.
- InvestigationTarget.cs:12 — 호적·밀서 조사 결과를 ClueManager 획득으로 연결.
- Docs/사건 정답지.md:38–45, :66–74 — 아래 판정표와 연출 규칙을 데이터로 반영.

| 조건 | 기획상 결과 |
|---|---|
| 설평 지목 + clue_household_registry_no_record + clue_wu_secret_letter_true_name 모두 보유 | 진엔딩 |
| 설평 지목 + 핵심 단서 하나 이상 부족 | 노멀 엔딩 |
| 다른 인물 지목 | 대상별 배드 엔딩 |
| 모순 미발견 | 이것만으로 실패하거나 진엔딩을 배제하지 않음 |
| 조사 기한 초과·미해결 | 배드 엔딩 방향. 구체적 처리·ID 미정 |

진술 모순은 신원 조사 경로를 여는 조건이며 진엔딩을 보장하지 않는다. 호적 부재 하나만으로 범인·첩자를 확정하지 않는다. 밀서의 구체적인 획득 경로는 현재 문서에도 충분히 정의되어 있지 않아 콘텐츠 설계가 필요하다.

연출 규칙: 진엔딩에서는 문서가 설평의 몸에 숨겨져 있고, 배드 엔딩에서는 이미 오나라에 전달되었으며, 노멀 엔딩은 행방 불명. 배드 엔딩에서 설평이 진범임을 명시적으로 공개하지 않는다. 기한 초과와 지목 확정이 겹칠 때의 판정 순서도 정해야 한다.

### P2 — 제작 도구와 검증 보강

- FirstInvestigationChecks.cs:17–79: 기존 검사는 도입부, 대화 중 이동 잠금, 대화 취소, 진술 우선 조사, 반복 조사, 수첩, 한글 폰트, 거리·차폐를 다룬다. 저장·스테이지 전환·엔딩 검사는 없다. 이번 점검에서는 실행하지 않았다.
- FirstInvestigationBuilder.cs:24–25, :28–106: 새 씬 생성만으로는 CommandTentBuilder가 자동 호출되지 않는다. 현재 저장된 씬에는 천막이 있지만, 새로 재생성할 때 도입부까지 재현하는 제작 경로는 보강할 필요가 있다.
- CommandTentBuilder.cs:19: introductionStage가 있으면 즉시 반환한다. 기존 씬 업데이트 작업은 이 도구의 재실행만으로 적용되지 않을 수 있다.
- FirstInvestigationBuilder.cs:154 이하, InvestigationView.cs:20–35: 새 UI를 추가할 때 계층 이름에 의존하는 참조와 제작 도구도 함께 갱신.

## 구현 후 확인할 핵심 시나리오

1. 새 게임 → 이름 입력 → 문서고 이동 → 두 조사 행동을 양쪽 순서로 수행.
2. 설평 진술 도중 취소하면 플래그가 생기지 않고, 끝까지 읽으면 한 번만 등록.
3. 조사 후 저장·종료·재실행 → 이름, 단서, 시간, 진행 위치 복원.
4. 단계 완료·결과 확인·선택 화면 진입·문서고 복귀 → 이름과 조사 기록 유지, 중복 전환 없음.
5. 설평 지목에 대해 핵심 증거 0개/각 1개/2개 조합을 검사: 노멀/노멀/진엔딩.
6. 다른 인물 지목과 기한 초과 결과를 검사하고, 모순 미발견만으로 강제 실패하지 않는지 확인.
7. 스테이지 이동 이후에도 최종 엔딩에 필요한 사건 증거가 보존되는지 확인.
8. 기존 씬 실행과 제작 도구로 새로 만든 씬의 필수 참조·빌드 씬 목록 확인.

현재 검토 순서: 첫 조사 완료 → 선택 패널 진입·복귀부터 구체화한다. 후속 씬으로 실제 이동하기 전 이름·단서·진술의 공통 상태를 마련하고, 이후 조건부 대화·후속 증거·최종 지목·엔딩을 연결한다.
