# 군기와 진영 소품 작업

## 완료 범위
중앙 군기만 Unity 적용. CanvasDetails·SupplyLoad·FieldTable·ArchiveShelf·CampFence는 전송 대기이며 완료 자산이 아니다.

## 편집 원본
- `CampStandardCrafted.blend`: 압축 저장, EDITABLE_CRAFT_PARTS에 개별 봉제·천·매듭·목재 부품. EDITABLE_VARCO_SOURCE에는 기존 생성 참조를 보존한다. 기본 숨김 상태.
- `CampStandard.blend`와 `Source/CampStandard/`: 최초 VARCO 생성 보정 및 원본. 지우지 않는다.
- 수작업은 원본을 다른 이름으로 저장한 뒤 진행한다. 스크립트는 수동 편집 내용을 자동 반영하지 않는다.

## 재현
아래 명령은 최초 작업 창 준비용이다. 이미 열린 작업 창이 있으면 새 프로세스를 실행하지 않는다. 재작업은 현재 창의 스크립트 편집기에서 수행하며, 재생성 전 현재 편집본을 별도 저장해야 한다.

초기 실행 형식:
`blender --python ArtSource/VarcoDetail20261014/bake_props.py -- CampStandard --crafted`
기존 출력이 있으면 중단한다. 출력과 생성용 master를 의도적으로 재생성할 때만 `--replace-generated` 추가. 이 스크립트는 현재 씬을 재생성한다. 저장하지 않은 편집 내용이 있으면 실행하지 않는다. 현재 작업 창 하나를 계속 재사용하며 `--background`는 사용하지 않는다.

`craft_standard.py`가 형상·부품별 재질을 만들고 `bake_props.py`가 단일 BakeUV 및 1K BaseColor/Normal/AO/MetalSmooth를 만든다. Cycles 12 samples, 12px padding, 금속 0, 최소 거칠기 .72, R=metal/A=1-roughness. FBX에는 첫 BakeUV만 사용, 원본은 압축 저장. 자동 백업은 기존 Git 제외 규칙 적용.

목재는 기존 ambientCG Wood049 CC0. VARCO 생성 참조 라이선스는 해당 서비스 라이선스이며 CC0가 아니다. 신규 이미지 생성이나 신규 유료 자산 구입 없음.

## Unity
`Assets/Art/Environment/VarcoDetailBaked/CampStandard.*` 및 4종 PNG.
Investigation 씬, Edit 모드에서 `Tools/Camp Environment/Apply Crafted Standard`. 현재 씬을 Assets/Logs/CraftedStandardReapply에 복사 보존한 뒤 기존 중앙 군기의 Renderer만 교체하고 충돌체·조사 객체는 유지한다. 재실행 시 같은 자식을 사용한다. 별도 인스턴스를 늘리지 않는다.

## 확인 자료
`size-comparison.json`, `CampStandard-bake-report.json`. 로컬 이미지·로그는 Logs/VarcoDetailReview20261014 및 Logs/EnvironmentRefresh/final-play-0.png. 전체 게임 성능이나 출시 품질 인증을 뜻하지 않는다.

## CampFence — 2026-10-14 완료
- 수신 원본: Source/BridgeIncoming/b93615db-9f72-4483-b5d1-3c1831b9a123/model.usda 및 원본 텍스처. VARCO 생성물이며 CC0가 아니다.
- 편집 원본: CampFence.blend, CampFence_Workbench 씬. 숨긴 EDITABLE_VARCO_SOURCE에 수신 메시·SourceUV를 보존한다.
- 제작 도구: Tools/finish_received_fence.py → bake_props.py의 CampFence --keep-scene. 기존 Blender 창의 콘솔에서 runpy로 실행했다. 현재 파일 백업은 Logs/VarcoFenceWork/before-*.blend에 있다. 완성 원본/FBX가 있으면 재실행을 거부한다. 추가 수작업은 완성 원본을 별도 버전으로 저장해 진행한다.
- 출력: Assets/Art/Environment/VarcoDetailBaked/CampFence.fbx 및 BaseColor/Normal/AO/MetalSmooth 1K PNG. 6,000삼각형, 단일 재질, Cycles 12 samples / 12px padding. 상세는 CampFence-bake-report.json.
- Unity: Investigation Edit 모드에서 Tools/Camp Environment/Apply Received Fence. 현재 씬을 Assets/Logs/ReceivedFence에 복사 백업하고 NorthFence만 교체한다. 기존 충돌체·조사 컴포넌트 유지. Capture Fence Close는 근접 렌더 확인용이다.
- 검증: Logs/VarcoFenceWork/unity-checks.txt 및 close.png, Logs/EnvironmentRefresh/final-play-0.png. Play 전체 화면·근접 렌더 확인, 직접 조작 이동 검사는 미실시.

## 추가 4종 수신과 일반 Git 관리
- CanvasDetails·SupplyLoad·FieldTable·ArchiveShelf 원본은 Source/<이름>/model.usda와 텍스처에 보존했다. 바르코 전송은 2026-10-14 11:41–11:44, 기존 Blender 창으로 완료됐다. 원래 수신 파일도 사용자 VARCO3D/Bridge/assets 폴더에 남아 있다.
- 기존 창에서 Tools/finish_received_props.py를 runpy로 실행한다. 현재 작업을 Logs/VarcoPropsWork에 백업하고 별도 씬에서 폭 방향 정렬·표면 보정·책상 상판과 접지 정리·1K 4종 베이킹을 순서대로 수행한다. 기존 출력이 있으면 중단한다. 아직 실행 완료가 확인되지 않았다.
- 이 폴더와 Assets/Art/Environment/VarcoDetailBaked는 일반 Git(-filter) 대상이다. 기존 LFS 이력을 변환하지 않았다. 원본 50MiB/파일, 게임용 10MiB/소품은 제작 예산이며 실제 근접 품질을 확인해야 한다. 다른 PC에서는 이 두 폴더·제작 도구·Unity .meta를 함께 전달한다. .blend1·캐시·Logs는 제외한다.
- Unity 적용 메뉴는 Tools/Camp Environment/Apply Four Received Props. 아직 미실행이다. 진행은 Logs/VarcoPropsWork/status.json과 unity-checks.txt로 확인한다.

## 현재 중단 지점과 재개 원칙
- 군기·북쪽 울타리: 기존 적용 결과 유지. 추가 4종: 수신 원본 보존 완료, 베이킹 시작 로그 없음. 같은 명령을 반복 입력하지 않는다.
- Tools/finish_received_props.py는 기존 프로젝트 Blender 창 내부에서만 실행한다. 실행되면 모델을 건드리기 전에 Logs/VarcoPropsWork/status.json에 preflight와 PID·현재 문서를 기록한다. 로그가 없으면 실행 확인 불가다. 파일을 지우거나 앱을 재시작하는 것으로 해결하지 않는다.
- preflight → backing_up → refining_and_baking → complete를 확인한 뒤 Unity 적용 메뉴를 실행한다. failed면 기록된 오류와 부분 산출물을 먼저 검토한다. 기존 출력은 자동 덮어쓰지 않는다.
- 정상 바르코 브릿지는 재사용한다. 새 작업 씬은 프로젝트 수신 경로를 사용한다. 작업용 추가 창·백그라운드 Blender를 만들지 않는다.

## 소품별 실행으로 변경
현재 Tools/finish_received_props.py는 한 번에 한 소품만 처리한다. 기본 대상은 CanvasDetails이며 자동으로 다음 소품을 실행하지 않는다. 다른 대상은 runpy.run_path의 init_globals에 PROP_NAME을 지정한다. 상태 로그는 Logs/VarcoPropsWork/<소품명>/status.json이다. 앞의 4종 일괄 실행 설명은 현재 방식으로 대체한다. 원본과 결과는 기존 소품별 파일명을 유지한다. 실제 실행과 품질 검증은 아직 미완료다.
