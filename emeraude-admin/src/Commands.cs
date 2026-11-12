using System.Linq;

namespace EmeraudeAdmin
{
    // Commandes de la console F5. Elles sont toutes envoyées au serveur, qui vérifie
    // que l'expéditeur est dans adminlist.txt avant de les exécuter.
    static class Commands
    {
        public const string Prefix = "adm:";

        public static readonly (string name, string usage)[] All =
        {
            ("joueurs", "liste les joueurs en jeu et leur position"),
            ("pos", "<joueur> : position d'un joueur"),
            ("inv", "<joueur> : inventaire d'un joueur (connu du serveur)"),
            ("tp", "<joueur> <joueur>  ou  <joueur> <x> <z> : téléporte un joueur"),
            ("give", "<joueur> <objet> [quantité] [qualité] : donne un objet (nom anglais du prefab)"),
            ("aide", ": liste des commandes"),
        };

        public static string Help =>
            "Commandes admin :\n" + string.Join("\n", All.Select(c => $"  {Prefix}{c.name} {c.usage}"));

        public static void Register()
        {
            foreach (var (name, usage) in All)
            {
                new Terminal.ConsoleCommand(Prefix + name, usage + " (admins, Emeraude Admin)", new Terminal.ConsoleEvent(args =>
                {
                    if (ZRoutedRpc.instance == null || ZNet.instance == null)
                    {
                        args.Context?.AddString("Connecte-toi d'abord au serveur.");
                        return;
                    }
                    string line = name + (args.Args.Length > 1 ? " " + string.Join(" ", args.Args.Skip(1)) : "");
                    ZRoutedRpc.instance.InvokeRoutedRPC(Net.Admin, line);
                }));
            }
        }
    }
}
