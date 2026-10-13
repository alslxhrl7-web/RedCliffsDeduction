# 다른 PC에서 적벽추리 작업 이어가기

기준: 2026-10-13 손 보정 종료 상태. 게임을 더 수정하지 않고, 완성된 보정과 원본을 이전하는 단계다. 현재 PC가 꺼져 있어도 아래 저장소와 설치 환경으로 작업할 수 있다.

보정 기준 커밋은 `6f2b1b5`이며 GitHub `main` 업로드를 완료했다. 새 LFS 객체 12개(432MB) 전송과 로컬 LFS 무결성을 확인했다. 이후 문서 커밋이 있을 수 있으므로 새 PC에서는 최신 `main`을 받는다.

## 받는 순서

1. 새 PC에서 GitHub 저장소 접근 권한이 있는 계정으로 로그인하고 Git 및 Git LFS를 설치한다. GitHub Desktop으로 Clone해도 된다. 웹의 Download ZIP 대신 Git 복제를 사용한다.
2. 원하는 폴더에서 아래 명령을 실행한다. 저장소 폴더 이름과 Windows 사용자 이름은 이전 PC와 같을 필요가 없다.

   ```sh
   git lfs install
   git clone https://github.com/alslxhrl7-web/RedCliffsDeduction.git
   cd RedCliffsDeduction
   git lfs pull
   git lfs fsck
   git log -1 --oneline
   ```

3. Unity Hub에서 Unity **6000.6.0f1**을 설치한다. Windows 실행 파일을 만들 때는 Windows 빌드 지원도 설치한다. `Assets`, `Packages`, `ProjectSettings`가 있는 저장소 루트를 연다. 패키지 다운로드와 첫 임포트를 기다린다.
4. `Assets/Scenes/Investigation.unity`를 열어 Play한다. Unity 실행에는 Blender나 VARCO 로그인이 필요하지 않다.
5. 조형 작업을 계속할 때는 Blender **5.2.2 LTS**를 설치하고 아래 원본을 연다. 원본에는 사용 텍스처가 패킹되어 있다.
6. Codex에는 이 저장소 루트 폴더를 프로젝트로 등록한다. Unity 연동이 필요하면 해당 PC의 Unity 플러그인·CLI 연결을 설정하고 `Window → Pipeline → Start Server`로 서버를 시작한다. Blender 자동화 연결도 새 PC에서 설정한다. 이전 PC의 인증 파일을 복사하지 않는다.

선택 검사: Python 3이 있으면 Unity 첫 임포트 후 `python Tools/verify-handoff.py`를 실행한다. 장면 의존 자산, Git 추적·메타, Blender 원본과 재현 입력을 검사한다. 커밋 보호 훅은 PowerShell에서 `Tools/install-commit-check.ps1`로 설치한다. .NET SDK가 있으면 `Tools/CompileCheck/compile-check.cmd`로 별도 코드 검사를 할 수 있다.

## 포함한 Blender 편집 원본

| 작업 | 원본 |
|---|---|
| 최종 손·손가락·손톱·소매 연결 | `ArtSource/HandRefinement20261013/Hands.blend` |
| 얼굴·인물 세부와 기존 역할 자세 | `ArtSource/CharacterDetail20261013/CharacterDetails.blend` |
| 인물별 작업 대기 자세·서류 | `ArtSource/RoleActivities20261013/RoleActivities.blend` |
| 조조 책상과 소품 | `ArtSource/CommandDesk20261013/CommandDesk_Baked.blend` |
| 천막·진영·CC0 소품 | `ArtSource/EnvironmentRefresh20261013/Environment_Set.blend` |

5개 원본을 실제로 열어 외부 연결 라이브러리와 패킹되지 않은 사용 텍스처가 없음을 확인했다. 게임용 베이크·메시·모션·재질·메타와 제작 코드도 함께 포함한다. 예전에 Git에서 제외한 CharacterRepair/ConversationMotions 폴더는 이번 작업 재개에 필수가 아니다.

인물 제작의 기준 입력 10개는 `ArtSource/RoleActivities20261013/SourceInputs.zip`에 보존했다. `build_activities.py`와 `build_detail.py`가 이 파일을 직접 읽는다. 작업 중 생성되는 `Input` 폴더를 새 기준으로 덮어쓰지 않는다. 외부 모션은 추출된 `external-motion.json`과 CC0 출처를 포함한다. 외부 모션 재추출·환경 전체 재생성에 쓰는 다운로드 캐시는 제외했으므로, 그 단계까지 다시 만들 때는 각 `sources.json`의 원본을 다시 받는다. 패킹된 편집 원본을 여는 데는 필요 없다.

손 제작은 `build_hands.py`가 CharacterDetails.blend에서 재생성한다. 기존 Hands.blend가 있으면 기본적으로 중단하며 `--replace-generated`를 명시해야 한다. **수작업한 원본을 별도 저장하지 않은 상태에서 재생성하지 않는다.** 중간 출력은 생성 후 Unity의 `Tools → Character Hands → Apply`로 저장된 Investigation 씬에 적용한다. Unity용 최종 자산은 이미 포함되어 있어 새 PC에서 Apply를 다시 누를 필요는 없다.

`Library`, `Temp`, `Logs`, `Builds`, `.blend1` 자동 백업과 Output/Preview는 복사·업로드 대상이 아니다. 별도 사운드 후보 `AudioLibrary`는 이번 인물·배경 보정 묶음의 범위에 포함하지 않는다.

## 어디까지 끝났는가

- 인물 10명·손 20개: 손가락 분리, 손목 연결, 네 손가락의 손톱 판과 관절 형상, 색상·노멀·매끄러움 베이킹.
- 네 손가락의 독립 이완 동작. 서류를 든 인물은 엄지·검지·중지의 접촉 자세를 고정한다.
- 이전 대기·대화 모션, 얼굴 보정과 책상·진영 보정을 유지했다. 현행 인물은 VarcoV3를 사용한다.
- Play 대기 검사 10명 통과: 손목 경계·발 이동 0mm, 빈손 손가락 약 3.4~5.3mm, 서류 손 약 0.5~0.6mm. 대화 중 연결 검사도 실패 0.
- 베이크 60장과 손가락 형태 80개 데이터 검사 통과. 직전 조사·이동·저장 회귀 검사 59개 통과.
- 이전 준비 중 별도 코드 컴파일 3종 오류·경고 0, 저장된 장면의 의존 자산 384개 및 Blender 원본 5개 검사 통과. Unity 브리지가 꺼져 있어 이전 준비 단계에서 Play를 추가로 재실행하지는 않았다.
- 이번 작업으로 배포 실행 파일을 새로 만들거나 itch에 게시하지 않았다. 새 PC 자체에서의 실행 확인은 아직 필요하다.

## 다음에 할 일과 작업 규칙

사용자는 이번 수정까지만 마치고 남은 보정을 나중에 이어가기로 했다. 지금 자동으로 새 조형 작업을 시작하지 않는다.

다음 보정 후보는 가까이서 보이는 피부의 균일한 질감, 엄지 손톱·손바닥 세부, 물건을 바꿔 잡는 동작이다. 현 상태를 출시 품질 완료로 판단하지 않는다. 기존 손목 경계 연결과 서류 접촉, 정점 수에 의존하는 동작 데이터를 유지해야 한다.

이후에도 Blender 원본 → UV → 베이킹 → Unity 적용 → 실제 실행 화면 확인 순서를 따른다. 원본·GUID·기존 조사 기능을 보존한다. 주요 변화만 패치노트에, 베이킹 문제는 `Docs/베이킹 문제.md`에 적는다. 상세한 구조와 검사는 `Docs/손가락 조형과 검증.md`를 읽는다.

## 대화를 이어가는 방법

GitHub는 프로젝트 파일을 옮기며, Codex 대화 자체를 옮기지는 않는다. 이 준비 시점에는 연결된 다른 PC가 없어 실제 대화 Hand off는 수행하지 않았다.

같은 대화를 옮기려면 두 PC가 연결된 동안 새 PC에 같은 저장소를 프로젝트로 등록하고 Codex의 Hand off로 인계한다. 현재 PC를 먼저 끈 상태에서 새 대화로 작업을 재개할 때는 아래 문장을 사용한다.

> 이 저장소의 AGENTS.md와 Docs/다른 PC 작업 인계.md, Docs/손가락 조형과 검증.md를 읽고 이전 작업 상태를 파악해. 우선 Git LFS 파일과 Unity 연결·컴파일·장면을 확인하고 결과만 알려줘. 새 조형 보정은 아직 시작하지 마. 프로젝트는 지금 열린 저장소 루트가 정본이야.

이 문서는 작업 재개용 요약이며 이전 대화 전체를 대체하는 완전한 대화 기록은 아니다.
