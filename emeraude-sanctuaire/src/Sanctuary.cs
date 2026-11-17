// La zone de la statue : une sphère sur la couche des déclencheurs de personnages, portée par une
// EffectArea du jeu.
//   PlayerBase : le jeu n'y fait apparaître aucun monstre (SpawnSystem.IsSpawnPointGood, y compris
//                pendant un raid, et CreatureSpawner) et les objets posés au sol n'y disparaissent pas.
//   NoMonsters : la zone du marchand ; un monstre qui s'en approche fuit (MonsterAI.UpdateAI).
//
// La statue n'est plus une pierre de garde (Plugin.AddStatue retire PrivateArea) : ce composant
// reprend l'infobulle et le cercle du rayon. Chaque statue a son rayon, enregistré dans son ZDO ;
// son créateur le change avec l'action secondaire (Maj + E).
using System;
using System.Text;
using UnityEngine;

namespace EmeraudeSanctuaire
{
    public class Sanctuary : MonoBehaviour, Hoverable, Interactable
    {
        internal const string PrefabName = "emeraude_sanctuaire";
        internal const string RadiusKey = "emeraude_sanctuaire_radius";
        static readonly float[] RadiusSteps = { 16f, 24f, 32f, 48f, 64f };

        // Repris de la pierre de garde sur le prefab ; Unity les rattache aux enfants de chaque copie.
        public CircleProjector m_areaMarker;
        public MeshRenderer m_model;
        public float m_hoverOffset;

        ZNetView m_nview;
        Piece m_piece;
        GameObject m_area;
        float m_radius;

        internal static float RadiusOf(ZDO zdo) => zdo.GetFloat(RadiusKey, Plugin.Radius.Value);

        void Awake()
        {
            m_nview = GetComponent<ZNetView>();
            m_piece = GetComponent<Piece>();
            // Le fantôme affiché pendant la construction n'a pas de ZDO : il montre le cercle, sans zone.
            if (!m_nview || !m_nview.IsValid())
            {
                SetMarkerRadius(Plugin.Radius.Value);
                return;
            }
            if (m_areaMarker) m_areaMarker.gameObject.SetActive(false);
            m_nview.Register<float>("EmeraudeSetRadius", RPC_SetRadius);
            ApplyRadius();
            InvokeRepeating(nameof(ApplyRadius), 1f, 1f);
        }

        // Suit le rayon du ZDO, changé ici ou chez un autre joueur.
        void ApplyRadius()
        {
            float radius = RadiusOf(m_nview.GetZDO());
            if (m_area && radius == m_radius) return;
            m_radius = radius;
            SetMarkerRadius(radius);

            // EffectArea lit son collider et son type dès son Awake (la zone NoMonsters garde la boîte
            // englobante de ce moment) : on la recrée, réglée objet inactif.
            if (m_area) Destroy(m_area);
            m_area = new GameObject("EmeraudeSanctuaireArea");
            m_area.SetActive(false);
            m_area.layer = LayerMask.NameToLayer("character_trigger");
            m_area.transform.SetParent(transform, false);
            m_area.AddComponent<SphereCollider>().radius = radius;
            var effect = m_area.AddComponent<EffectArea>();
            effect.m_type = EffectArea.Type.PlayerBase;
            if (Plugin.RepelMonsters.Value) effect.m_type |= EffectArea.Type.NoMonsters;
            m_area.SetActive(true);
        }

        void SetMarkerRadius(float radius)
        {
            if (m_areaMarker) m_areaMarker.m_radius = radius;
        }

        // Le cercle s'affiche tant qu'on regarde la statue, comme pour une pierre de garde.
        void ShowMarker()
        {
            if (!m_areaMarker) return;
            m_areaMarker.gameObject.SetActive(true);
            CancelInvoke(nameof(HideMarker));
            Invoke(nameof(HideMarker), 0.5f);
        }

        void HideMarker() => m_areaMarker.gameObject.SetActive(false);

        // Seul le propriétaire du ZDO peut l'écrire : le créateur demande, le propriétaire applique.
        void RPC_SetRadius(long sender, float radius)
        {
            if (m_nview.IsOwner()) m_nview.GetZDO().Set(RadiusKey, radius);
        }

        public string GetHoverText()
        {
            if (!m_nview.IsValid() || !Player.m_localPlayer) return "";
            ShowMarker();
            var text = new StringBuilder("$emeraude_sanctuaire");
            text.Append("\n$emeraude_sanctuaire_radius");
            if (m_piece.IsCreator())
                text.Append("\n[<color=yellow><b>$KEY_AltPlace + $KEY_Use</b></color>] $emeraude_sanctuaire_change_radius");
            return Localization.instance.Localize(text.ToString(), m_radius.ToString("0"));
        }

        public string GetHoverName() => "$emeraude_sanctuaire";

        public float GetHoverOffset() => m_hoverOffset;

        public bool Interact(Humanoid user, bool hold, bool alt)
        {
            if (hold || !alt || !m_nview.IsValid() || !m_piece.IsCreator()) return false;
            int next = Array.FindIndex(RadiusSteps, step => step > m_radius + 0.1f);
            float radius = RadiusSteps[next < 0 ? 0 : next];
            m_nview.InvokeRPC("EmeraudeSetRadius", radius);
            SetMarkerRadius(radius);
            ShowMarker();
            user.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$emeraude_sanctuaire_radius", radius.ToString("0")));
            return true;
        }

        public bool UseItem(Humanoid user, ItemDrop.ItemData item) => false;
    }
}
