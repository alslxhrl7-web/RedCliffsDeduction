# 조조 Enemies 헤어 — 2026-10-18

## 2026-10-18 사용 범위 정정

동봉 `LocalArchive/CaoCaoHairEnemies20261016/Source/Assets/license.txt`의 Exhibit A 2.2.1은 해당 인간 자산을 비상업적 교육 용도로 제한한다. 사용자는 별도 상업 허가가 없다고 확인하고 직접 제작 헤어 교체를 지시했다. 아래 Enemies 기반 제작 내역은 과거 이력이며 판매용 사용 허가를 의미하지 않는다.

기존 패키지·추출 데이터·Blender 원본은 LocalArchive에 보존한다. 새 헤어는 Enemies 메시·커브·텍스처를 사용하지 않고 독립적인 형상과 UV 기반 재질로 제작한다. 교체 원본은 `LocalArchive/CaoCaoHairEnemies20261016/Revision12/CaoCao_OriginalHair.blend`이며 게임 적용·검증 결과는 [인물 작업 기록](인물%20전체%20파츠%201차%20정리%202026-10-18.md)에 기록한다.


## 이전 적용 이력: 머릿결·상투 비율 재수정

- 사용자 검토에서 이전 버전이 덩어리처럼 보인다는 피드백을 받아 재수정했다. 상투 폭 약 32%, 깊이 36%, 높이 37% 축소. 두피의 거친 면을 완화하고, 기존 Enemies 기반 형태/UV 위에 굽은 큰 결·잔결을 색상과 탄젠트 노멀로 베이킹했다. 묶음 띠를 추가했다.
- 현재 원본: `C:\Users\나민재\Documents\RedCliffsDeduction\LocalArchive\CaoCaoHairEnemies20261016\Revision2\CaoCaoHair_Detailed.blend`. 이전 `.blend`와 `Revision2/PreviousGameAssets`를 보존한다. 새 원본도 LocalArchive 제외 정책을 유지한다.
- Cycles 24 samples, margin 12px. 2K로 구운 뒤 게임용 1K로 필터링. 원본 2K는 `Revision2/Bakes2K`에 보관한다. 뒤통수 형상 보정 후 해당 두피 3개 맵은 1K에서 다시 베이킹했으며 현재 게임 파일과 수정 원본에 보관한다. BaseColor는 직접광/간접광 제외. 균일 비금속/거칠기는 상수 사용.
- 최종 URP smoothness 0.4, normal strength 0.85, occlusion strength 0.3. FBX+PNG 합계 8,076,081 bytes (약 7.70MiB). 기존 초미세 보조 메시 대신 베이크된 결을 사용한다.
- 원래 몸 메시의 큰 상투는 Blender에서 축소한 변환을 기존 포즈 메시 복사본 `EnemiesHair/CaoCao_RolePose_HairRefit.asset`에 재현했다. 원래 `CaoCao_RolePose0.asset`와 UV·몸 자세·상호작용은 보존한다. 복사본은 항상 원본에서 재생성하므로 반복 적용 시 중복 축소하지 않는다.
- 원본의 최대 높이가 줄어 입모양 위치까지 내려가는 것을 막기 위해 헤어 표시 컴포넌트에 원래 몸 높이 1.78m를 보존한다.
- 첫 게임 검사: 정면 결/크기는 개선됐으나 뒤쪽에 원래 머리가 드러남. Blender 뒤통수 외피를 바깥쪽 교점으로 보정해 1,744개 정점을 다시 맞췄다. 엔진 재검사에서도 남은 어긋남은 기존 조조의 1.5도 기울기 때문이었다. 기존 installer와 같은 골반 기준 회전을 헤어에 적용하고 상투 축소도 정자세 공간에서 수행해 해결했다.
- 최종 Play 모드 검사: 정면·측면·후면 3개 화면에서 머릿결, 상투 비율, 이마/뒤통수 접합 확인. `Revision2/game-front-final.png`, `game-side-final.png`, `game-rear-final.png`. 마지막 화면 검토 이후 추가 형상 변경 없음.
- 움직임 최대 오차 0.00000274m 미만, 헤어 충돌체 0개. 기존 `대화 입모양 검증`과 `인물 대기 움직임 검증` 각 10명 PASS. 조조 입모양 54 정점 변형 및 원위치 복귀 통과. 로그는 `Logs/VarcoPreviews/speech-check.txt`, `idle-check.txt`.
- 검토 카메라 원위치 복원 후 Play 모드만 종료했다. Unity·Blender·웨일 창 유지. 커밋·업로드·새 빌드 없음.

## 최초 적용 이력 (아래 수치는 이전 버전)

- 적용 프로젝트: `C:\Users\나민재\Documents\RedCliffsDeduction` (사용자가 선택한 현재 Unity 프로젝트).
- 원본: 보유 에셋 `Enemies | 3D Sample Project (2023)`, Asset Store 248975. `OLD_LOWRES_Anne_Groom_female3_Optimized_Left.asset`의 62,737개 모발 루트·커브 데이터를 추출하고 조조 두피·상투에 맞춰 재배치했다. 데모 전체나 HDRP 설정은 임포트하지 않았다.
- 편집 원본 인계: `C:\Users\나민재\Documents\RedCliffsDeduction\LocalArchive\CaoCaoHairEnemies20261016\CaoCaoHair_Enemies_Workbench.blend` (27,107,551 bytes). 같은 폴더의 `Source`, `source-map.json`, `anne-left-groom-source.json`, 제작 스크립트와 보고서를 함께 보존한다. 이 LocalArchive는 기존 Git 제외 정책을 유지한다.
- 라이선스 원문 보관: 위 `Source/Assets/license.txt`, `Source/Assets/_Packages/com.unity.demoteam.hair/LICENSE.md`. 원본 패키지는 Unity Asset Store 캐시에 남아 있다.
- 도구: 보이는 Blender 5.2.2 창에서 모델링·UV·Cycles 베이킹·FBX 출력. Unity 6000.6.0f1의 열린 Editor에서 적용. 앱을 종료하거나 재실행하지 않았다.
- 출력: `Assets/Art/Characters/VarcoV3/CaoCao/EnemiesHair`. FBX 및 PNG 합계 7,189,979 bytes (약 6.86MiB). 새 FBX는 해당 폴더 `.gitattributes`로 일반 Git을 사용한다. 기존 LFS 이력은 변경하지 않았다.
- 베이킹: 두피/상투 각각 1024² BaseColor·탄젠트 Normal·AO, Cycles 16 samples, margin 8px, selected-to-active, cage extrusion 0.004m, ray distance 0.012m. BaseColor에 직접광·간접광을 굽지 않았다. 비금속 0, 균일 roughness 0.68은 상수로 처리해 별도 맵을 생략했다.
- 게임 재질: URP Lit, normal strength 0.65, occlusion strength 0.35. 초미세 보조 모발 메시가 점무늬로 보이므로 게임에서는 비활성화하고 베이킹된 표면을 사용한다. 원본과 선택용 메시를 삭제하지 않았다.
- 대상: `CaoCaoVisual.prefab` 및 `Investigation` 씬의 `CaoCao/VarcoVisual/EnemiesHair`. 기존 몸·포즈 메시·GUID·충돌체를 유지했다. FBX와 기존 포즈의 정면 축 차이를 Y 180도로 보정했다.
- 움직임: `VarcoHairAttachment` 표시가 있는 헤어에는 기존 머리 부분과 동일한 최대 호흡/흔들림을 적용하며 입술 변형은 적용하지 않는다. 다른 캐릭터의 계산은 유지한다.

## 검증

- 최초 근접 검사 실패: 앞뒤 축 반대로 얼굴 앞에 헤어가 보임 → 방향 수정 → 정면/측면 재검사 통과.
- 다음 근접 검사 실패: 가는 보조 모발에 점무늬 → 그림자/반사 조정만으로 충분하지 않아 보조 모발 비활성화 → 최종 정면/측면에서 제거 확인.
- Play 모드에서 실제 게임 카메라·천막 조명으로 최종 정면/측면 PNG 확인. 기본 게임 시작·이름 선택·조조 대화 진입 확인. 헤어 추가 충돌체 0개.
- 머리 움직임 검산: 두 표면 메시의 변위를 시간 0/1초에 비교, 최종 최대 오차 약 0.0000013m. 기록 `runtime-front.txt`, `runtime-side.txt`.
- 증거: LocalArchive 작업 폴더의 `game-front-final.png`, `game-side-final.png`, `bake-report.json`. 전체 게임 동선·모든 엔딩 회귀 검사는 이번 범위가 아니다.
- 근접 모발 한 올의 표현은 베이킹 결과로 단순화되어 있다. 기존 얼굴·수염·갑옷의 조형 품질이나 전체 인물의 상용화 품질을 완료로 판정하지 않는다.
- 커밋·업로드·배포는 수행하지 않았다.

