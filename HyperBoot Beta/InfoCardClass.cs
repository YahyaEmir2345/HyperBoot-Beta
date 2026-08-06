using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using HartUI.Controls;

public class InfoCardClass
{
    public const int DefaultX = 15;
    public const int DefaultY = 14;
    public const int SpacingX = 99;
    public const int SpacingY = 92;

    private const int CardWidth = 96;
    private const int CardHeight = 82;
    private const int LabelMargin = 3;

    // Renk paleti (istediğiniz gibi özelleştirin)
    private static readonly Color NormalColor = Color.Gainsboro;
    private static readonly Color HoverColor = Color.LightSteelBlue;
    private static readonly Color PressedColor = Color.SteelBlue;

    public List<cuiPanel> Cards { get; } = new List<cuiPanel>();

    // Hangi karta tıklandığını dışarı bildirmek için
    public event EventHandler<CardClickedEventArgs> CardClicked;

    public InfoCardClass(Control targetControl, IEnumerable<string> cardTexts)
    {
        if (targetControl == null)
            throw new ArgumentNullException(nameof(targetControl));

        var texts = cardTexts?.ToList() ?? new List<string>();

        try
        {
            targetControl.SuspendLayout();

            if (targetControl is ScrollableControl container)
                container.AutoScroll = true;

            if (targetControl is Panel p)
                p.AutoScrollPosition = new Point(0, 0);

            int availableWidth = targetControl.ClientSize.Width - DefaultX;
            int cardsPerRow = Math.Max(1, availableWidth / SpacingX);

            for (int i = 0; i < texts.Count; i++)
            {
                int index = i; // closure için local kopya

                var infoCard = new cuiPanel
                {
                    Size = new Size(CardWidth, CardHeight),
                    BackColor = Color.Transparent,
                    PanelColor = NormalColor,
                    Cursor = Cursors.Hand,
                    Tag = index
                };

                var label = new Label
                {
                    Text = texts[i] ?? string.Empty,
                    Location = new Point(LabelMargin, LabelMargin),
                    Size = new Size(CardWidth - LabelMargin * 2, CardHeight - LabelMargin * 2),
                    Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
                    AutoEllipsis = true,
                    TextAlign = ContentAlignment.TopLeft,
                    BackColor = Color.Transparent,
                    Cursor = Cursors.Hand
                };

                infoCard.Controls.Add(label);

                // --- Hover & Click efektleri ---
                EventHandler onEnter = (s, e) => infoCard.PanelColor = HoverColor;
                EventHandler onLeave = (s, e) => infoCard.PanelColor = NormalColor;
                MouseEventHandler onDown = (s, e) => infoCard.PanelColor = PressedColor;
                MouseEventHandler onUp = (s, e) =>
                {
                    // Mouse hâlâ kart üzerindeyse hover rengine, değilse normale dön
                    bool stillInside = infoCard.ClientRectangle.Contains(infoCard.PointToClient(Cursor.Position));
                    infoCard.PanelColor = stillInside ? HoverColor : NormalColor;
                };
                EventHandler onClick = (s, e) =>
                {
                    CardClicked?.Invoke(this, new CardClickedEventArgs(index, infoCard, texts[index]));
                };

                // Panel'in kendisine ekle
                infoCard.MouseEnter += onEnter;
                infoCard.MouseLeave += onLeave;
                infoCard.MouseDown += onDown;
                infoCard.MouseUp += onUp;
                infoCard.Click += onClick;

                // Label panelin üstünde olduğu için mouse olayları label'a düşer,
                // bu yüzden aynı davranışı label'a da bağlıyoruz.
                label.MouseEnter += onEnter;
                label.MouseLeave += onLeave;
                label.MouseDown += onDown;
                label.MouseUp += onUp;
                label.Click += onClick;

                int col = index % cardsPerRow;
                int row = index / cardsPerRow;

                infoCard.Location = new Point(
                    DefaultX + col * SpacingX,
                    DefaultY + row * SpacingY);

                targetControl.Controls.Add(infoCard);
                infoCard.BringToFront();
                Cards.Add(infoCard);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("Kartlar oluşturulurken hata: " + ex.Message);
        }
        finally
        {
            targetControl.ResumeLayout(true);
        }
    }

    public InfoCardClass(Control targetControl, int cardcount, string cardText = "")
        : this(targetControl, Enumerable.Repeat(cardText, cardcount))
    {
    }

    public void Clear(Control targetControl)
    {
        foreach (var card in Cards)
        {
            targetControl.Controls.Remove(card);
            card.Dispose();
        }
        Cards.Clear();
    }
}

public class CardClickedEventArgs : EventArgs
{
    public int Index { get; }
    public cuiPanel Card { get; }
    public string Text { get; }

    public CardClickedEventArgs(int index, cuiPanel card, string text)
    {
        Index = index;
        Card = card;
        Text = text;
    }
}