namespace P.E.A.K_MENU.UI;

internal interface IMenuPage
{
    string Title { get; }

    void Draw(MenuStyles styles);
}