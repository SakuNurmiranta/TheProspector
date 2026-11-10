using System;
using UnityEngine;

namespace SEMM91.Presentation
{
    [Serializable]
    public sealed class CirculationVisualPalette
    {
        [Header("Node Colors")]
        public Color SelectedNode =
            new Color(1.00f, 0.90f, 0.25f);

        public Color OutgoingContact =
            new Color(0.40f, 0.95f, 0.66f);

        public Color NotReceived =
            new Color(0.58f, 0.64f, 0.70f);

        public Color ReceivedUnopened =
            new Color(1.00f, 0.80f, 0.30f);

        public Color Queued =
            new Color(1.00f, 0.55f, 0.20f);

        public Color ListeningStart =
            new Color(0.35f, 0.76f, 1.00f);

        public Color ListeningEnd =
            new Color(0.15f, 0.48f, 0.90f);

        public Color Completed =
            new Color(0.20f, 0.82f, 0.51f);

        [Header("Connection Colors")]
        public Color NormalConnection =
            new Color(0.45f, 0.53f, 0.66f);

        public Color HighlightedConnection =
            new Color(1.00f, 0.78f, 0.30f);

        [Header("Scene Colors")]
        public Color SceneBackground =
            new Color(0.065f, 0.085f, 0.12f);

        public Color NodeLabels = Color.white;

        [Header("GUI Colors")]
        public Color PanelBackground =
            new Color(0.035f, 0.055f, 0.09f);

        public Color PanelText =
            new Color(0.95f, 0.97f, 1.00f);

        public Color ButtonTint =
            new Color(0.24f, 0.36f, 0.53f);
    }
}