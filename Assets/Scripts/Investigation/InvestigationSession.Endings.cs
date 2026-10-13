using UnityEngine.SceneManagement;

namespace RedCliffMystery.Investigation
{
    public enum InvestigationEnding { None = 0, True = 1, Normal = 2, Unresolved = 6 }

    public sealed partial class InvestigationSession
    {
        public InvestigationEnding CurrentEnding { get; private set; }

        void ConfirmRestart()
        {
            SetModal("다시 시작", "이번 조사를 끝내고 처음으로 돌아갑니까?",
                "이름 입력부터 다시 시작합니다. 저장 슬롯은 유지되며 새 조사의 진행은 자동저장됩니다.");
            onEscape = ShowOrderEnding;
            view.AddChoice("처음부터 시작", () => SceneManager.LoadScene(SceneManager.GetActiveScene().path));
            view.AddChoice("결과 화면으로 돌아가기", ShowOrderEnding);
        }
    }
}
