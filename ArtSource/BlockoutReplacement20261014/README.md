# 기본 도형 교체 — 2026-10-14

Investigation 씬에서 보이던 기본 메시 273개를 Blender 제작 키트 22종으로 교체했다. 삭제 대신 기존 MeshFilter와 재질만 교체해 오브젝트 ID, 충돌체 665개, 조사 대상 20개를 유지한다. 기존 목패의 별도 눈금 3개는 새 목패의 눈금과 중복되므로 표시를 끈다. 비활성 보관 오브젝트와 다른 씬까지 전부 교체했다는 뜻은 아니다.

## 편집과 재현

- 이 폴더의 각 `.blend`: 부품별 편집 컬렉션, 게임용 결합 메시, UV와 재질, 검토 카메라를 포함한다. 원본은 일반 Git으로 보관한다.
- `placements.json`: 변경 전 GlobalObjectId, 크기, 재질, 분류. 덮어쓰지 않는다.
- `Recipes/*.json`: 제작 대상과 분류. `*-report.json`: 베이킹 맵·정점·삼각형 수.
- `Tools/build_crafted_blockout.py`: 개별 키트 조형과 베이킹. 기존 결과가 있으면 중단하므로 다음 수정은 별도 리비전에서 한다.
- 연결된 기존 Blender 창에 `Tools/request_blender_job.py refine_prop --prop Kit_<분류>`로 하나씩 요청하고 렌더를 검토한다. 자동 연속 제작은 하지 않는다.
- Unity 출력: `Assets/Art/Environment/BlockoutReplacement20261014/<분류>`. geometry.json은 Blender 좌표를 Unity 좌표로 바꾼 중간 교환 자료이며 실제 씬은 Mesh.asset을 사용한다.
- `Assets/Editor/BlockoutReplacement.cs`: 검토한 키트 적용, 씬 렌더, 연결 검사. `Tools/Blockout Replacement` 메뉴에서 실행한다. 적용은 기존 자산 GUID를 유지하며 재질 설정도 갱신한다.
- 로컬 검토 렌더·로그: `Logs/BlockoutReplacement`. 최초 씬 사본: `Assets/Logs/BlockoutBackup`. Git 제외 대상이다.

## 제작과 라이선스

이번 키트는 VARCO 신규 생성물이 아니라 기존 Blender 창에서 직접 절차 조형했다. 판재·이음새·목심·새끼줄·종이 겹·두루마리 단면·등불 살대·지형 굴곡을 분리 제작했다. 텍스처는 프로젝트에 이미 반입된 ambientCG CC0 Wood049, Fabric030, Ground054를 재사용했다. 각각 https://ambientcg.com/view?id=Wood049 , https://ambientcg.com/view?id=Fabric030 , https://ambientcg.com/view?id=Ground054 . 라이선스 기록은 기존 CommandTent/Textures 출처 문서를 유지한다. 신규 외부 모델 다운로드는 없다.

## 베이킹과 용량

1K BaseColor·탄젠트 Normal·AO·MetalSmooth, Cycles 12 samples, 여백 10px. 색상에 직접광·간접광을 굽지 않는다. MetalSmooth의 R은 금속, A는 매끄러움이며 최대 0.45다. 등불은 Emission 마스크를 추가했다. 강 수면은 새 메시를 쓰되 기존 수면 재질을 유지하며 생성된 토양 맵을 수면에 연결하지 않는다.

편집 원본과 출력 합계 약 75.5MiB, Unity 폴더 약 45.2MiB, 가장 큰 개별 파일 약 2.7MiB다. 키트별 게임 메시와 PNG 합계 최대 약 3.1MiB이며 LFS는 사용하지 않는다. 빌드 압축 용량과 메모리 사용량은 이 파일 크기와 다르다.

## 검토 범위

Blender 렌더를 키트별로 확인하고 실제 Unity 씬에서 문서고·등불·문서·수군 진영·통로를 확인했다. 기본 도형 연결 273개와 남은 활성 기본 메시 0개를 검사한다. 근접 목재 무늬의 반복과 종이 묶음의 세부 표현은 출시 전 추가 조형·아트 디렉션 검토 여지가 있다. 기본 도형 교체 완료를 상용화 품질 검수 완료와 동일하게 취급하지 않는다.
