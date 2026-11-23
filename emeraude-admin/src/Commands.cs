using System.Linq;

namespace EmeraudeAdmin
{
    // Commandes de la console F5. Elles sont toutes envoyées au serveur, qui vérifie
    // que l'expéditeur est dans adminlist.txt avant de les exécuter.
    // Chaque commande a un nom anglais (envoyé au serveur) et un alias français.
    static class Commands
    {
        public const string Prefix = "adm:";

        public static readonly (string name, string alias, string usageEn, string usageFr)[] All =
        {
            ("players", "joueurs", "lists the players in game and their positions", "liste les joueurs en jeu et leur position"),
            ("pos", null, "<player> : position of a player", "<joueur> : position d'un joueur"),
            ("inv", null, "<player> : inventory of a player (as known by the server)", "<joueur> : inventaire d'un joueur (connu du serveur)"),
            ("tp", null, "<player> <player>  or  <player> <x> <z> : teleports a player", "<joueur> <joueur>  ou  <joueur> <x> <z> : téléporte un joueur"),
            ("give", null, "<player> <item> [amount] [quality] : gives an item (English prefab name)", "<joueur> <objet> [quantité] [qualité] : donne un objet (nom anglais du prefab)"),
            ("portal", "portail", "<player> <portal name>  or  no argument : teleports a player to a portal, or lists the portals", "<joueur> <nom du portail>  ou  sans argument : téléporte un joueur sur un portail, ou liste les portails"),
            ("take", "retirer", "<player> <item> [amount] : removes an item from a player's inventory", "<joueur> <objet> [quantité] : retire un objet de l'inventaire d'un joueur"),
            ("skill", "competence", "<player> <skill> <level> : sets a skill level (0 to 100)", "<joueur> <compétence> <niveau> : règle le niveau d'une compétence (0 à 100)"),
            ("heal", "soigner", "<player> : full health, stamina and eitr, removes burning, poison, wet, cold…", "<joueur> : vie, endurance et eitr au maximum, retire brûlure, poison, mouillé, froid…"),
            ("repair", "reparer", "<player> : repairs all the player's items", "<joueur> : répare tous les objets du joueur"),
            ("god", "dieu", "[player] [on|off] : invincible mode (yourself by default), kept after death and reconnection until off", "[joueur] [on|off] : mode invincible (toi par défaut), gardé après une mort ou une reconnexion jusqu'à off"),
            ("ghost", "fantome", "[player] [on|off] : monsters ignore the player (yourself by default), kept like god", "[joueur] [on|off] : les monstres ignorent le joueur (toi par défaut), gardé comme dieu"),
            ("fly", "vol", "[player] [on|off] : fly mode, jump to go up, crouch to go down (yourself by default)", "[joueur] [on|off] : mode vol, saut pour monter, accroupi pour descendre (toi par défaut)"),
            ("damage", "degats", "[player] <multiplier> : multiplies the damage the player deals, 1 = normal (yourself by default, until the game is closed)", "[joueur] <multiplicateur> : multiplie les dégâts infligés par le joueur, 1 = normal (toi par défaut, jusqu'à la fermeture du jeu)"),
            ("tomb", "tombe", "<player> [number] : lists the player's tombstones, or teleports the player to one", "<joueur> [numéro] : liste les tombes du joueur, ou le téléporte sur l'une d'elles"),
            ("journal", null, "<player> [lines] : last lines of a player's journal (20 by default)", "<joueur> [lignes] : dernières lignes du journal d'un joueur (20 par défaut)"),
            ("ores", "minerais", "[player] : ore leaderboard, or a player's details (Emeraude Minerais)", "[joueur] : classement des minerais, ou le détail d'un joueur (Emeraude Minerais)"),
            ("say", "dire", "<message> : shows a message in the middle of every player's screen", "<message> : affiche un message au centre de l'écran de tous les joueurs"),
            ("day", "jour", ": skips to the morning", ": passe au matin"),
            ("night", "nuit", ": skips to the evening", ": passe au soir"),
            ("raid", null, "<player> [raid]  or  stop : starts a raid near a player (no raid name: lists them), or stops the current one", "<joueur> [raid]  ou  stop : lance un raid près d'un joueur (sans nom : la liste), ou arrête celui en cours"),
            ("kick", "expulser", "<player> [reason] : kicks a player out of the server", "<joueur> [raison] : expulse un joueur du serveur"),
            ("help", "aide", ": lists the commands", ": liste des commandes"),
        };

        // Les anciennes versions (1.0 à 1.2) envoyaient les noms français
        public static string Canonical(string cmd) =>
            All.FirstOrDefault(c => c.name == cmd || c.alias == cmd).name ?? cmd;

        public static string Help =>
            L.T("Admin commands:", "Commandes admin :") + "\n" +
            string.Join("\n", All.Select(c => $"  {Prefix}{(L.Fr && c.alias != null ? c.alias : c.name)} {L.T(c.usageEn, c.usageFr)}"));

        public static void Register()
        {
            foreach (var c in All)
            {
                var console = new Terminal.ConsoleEvent(args =>
                {
                    if (ZRoutedRpc.instance == null || ZNet.instance == null)
                    {
                        args.Context?.AddString(L.T("Connect to the server first.", "Connecte-toi d'abord au serveur."));
                        return;
                    }
                    string line = c.name + (args.Args.Length > 1 ? " " + string.Join(" ", args.Args.Skip(1)) : "");
                    ZRoutedRpc.instance.InvokeRoutedRPC(Net.Admin, line);
                });
                string description = L.T(c.usageEn, c.usageFr) + " (admins, Emeraude Admin)";
                new Terminal.ConsoleCommand(Prefix + c.name, description, console);
                if (c.alias != null) new Terminal.ConsoleCommand(Prefix + c.alias, description, console);
            }
        }
    }
}
