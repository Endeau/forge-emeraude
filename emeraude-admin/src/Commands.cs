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
