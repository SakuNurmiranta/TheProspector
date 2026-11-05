
using SEMM91.Domain;
using UnityEngine;

public sealed class Semm91DemoView : MonoBehaviour
{
    private DemoSession _session;

    private void Awake()
    {
        _session = DemoSession.CreateDefault();
    }

    private void OnGUI()
    {
        if (_session == null)
            return;

        float height = Mathf.Min(
            Screen.height - 32f, 450f);

        if (height <= 0f)
            return;

        GUILayout.BeginArea(
            new Rect(16f, 16f, 420f, height),
            GUI.skin.window);

        GUILayout.Label("SEMM91 - Simulation Demo");

        GUILayout.Space(8f);

        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Uusi ajo"))
            _session = DemoSession.CreateDefault();

        if (GUILayout.Button("+1 paiva"))
            _session.AdvanceDay();

        if (GUILayout.Button("+10 paivaa"))
            _session.AdvanceDays(10);

        GUILayout.EndHorizontal();

        GUILayout.Space(12f);

        GUILayout.Label(
            "Simulaatiopaiva: " + _session.Day);

        GUILayout.Label(
            "Kentan SIR-tila: " +
            _session.FieldState);

        GUILayout.Label(
            "Aktiiviset kopioijat: " +
            _session.ActiveCopiers);

        GUILayout.Label(
            "Toimitettu levikki: " +
            _session.DeliveredReach.ToString("P1"));

        GUILayout.Label(
            "Avatut viestit: " +
            _session.OpenedMessageCount);

        GUILayout.Label(
            "B:n lahettamat kopiot: " +
            _session.BCopyShipments);

        GUILayout.Space(12f);

        GUILayout.Label(
            "B:n kuuntelu: " +
            ListeningLabel("B"));

        GUILayout.Label(
            "C:n kuuntelu: " +
            ListeningLabel("C"));

        GUILayout.Space(12f);

        GUILayout.Label(
            "D:n passiivinen kenttaloyto: " +
            (_session.DHasDiscovered
                ? "Kylla"
                : "Ei"));

        GUILayout.Space(12f);

        GUILayout.Label(
            "Tama nakyma esittaa tapahtumia " +
            "ja SIR-tiloja, ei legitimiteettia.");

        GUILayout.EndArea();
    }

    private string ListeningLabel(string nodeId)
    {
        var phase = _session.GetListeningPhase(nodeId);

        return phase.HasValue
            ? phase.Value.ToString()
            : "Ei alkanut";
    }
}
