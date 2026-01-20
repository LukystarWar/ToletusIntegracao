using System.Net;
using Toletus.LiteNet2.Base;
using Toletus.LiteNet2.Command.Enums;

Console.Title = "Demo Catraca LiteNet2";
Console.Clear();

Console.WriteLine("╔════════════════════════════════════════════════════════╗");
Console.WriteLine("║      DEMO - INTEGRAÇÃO CATRACA TOLETUS LITENET2        ║");
Console.WriteLine("╚════════════════════════════════════════════════════════╝");
Console.WriteLine();

// Configuração
var catracaIp = "192.168.18.200";

Console.Write($"IP da catraca [{catracaIp}]: ");
var inputIp = Console.ReadLine();
if (!string.IsNullOrWhiteSpace(inputIp))
    catracaIp = inputIp;

Console.WriteLine();
Console.WriteLine($"Conectando em {catracaIp}...");

var ip = IPAddress.Parse(catracaIp);
var catraca = new LiteNet2BoardBase(ip);

try
{
    catraca.Connect();
    Console.WriteLine("Conectado com sucesso!");
    Console.WriteLine();

    // Garantir estado inicial (azul/controlado)
    catraca.Send(Commands.SetFlowControlExtended, (byte)2);

    while (true)
    {
        Console.WriteLine("┌────────────────────────────────────────┐");
        Console.WriteLine("│           MENU DE DEMONSTRAÇÃO         │");
        Console.WriteLine("├────────────────────────────────────────┤");
        Console.WriteLine("│  1 - LIBERAR ENTRADA (LED Verde)       │");
        Console.WriteLine("│  2 - LIBERAR SAÍDA (LED Verde)         │");
        Console.WriteLine("│  3 - ACESSO NEGADO (LED Vermelho 3s)   │");
        Console.WriteLine("│  4 - BLOQUEAR (LED Vermelho permanente)│");
        Console.WriteLine("│  5 - VOLTAR AO NORMAL (LED Azul)       │");
        Console.WriteLine("│  0 - SAIR                              │");
        Console.WriteLine("└────────────────────────────────────────┘");
        Console.WriteLine();
        Console.Write("Escolha: ");

        var opcao = Console.ReadKey();
        Console.WriteLine();
        Console.WriteLine();

        switch (opcao.KeyChar)
        {
            case '1':
                Console.WriteLine(">>> LIBERANDO ENTRADA...");
                catraca.Send(Commands.ReleaseEntry);
                Console.WriteLine("    LED VERDE - Entrada liberada!");
                break;

            case '2':
                Console.WriteLine(">>> LIBERANDO SAÍDA...");
                catraca.Send(Commands.ReleaseExit);
                Console.WriteLine("    LED VERDE - Saída liberada!");
                break;

            case '3':
                Console.WriteLine(">>> ACESSO NEGADO (3 segundos)...");
                catraca.Send(Commands.SetFlowControlExtended, (byte)8);
                Console.WriteLine("    LED VERMELHO - Bloqueado!");
                Thread.Sleep(3000);
                catraca.Send(Commands.SetFlowControlExtended, (byte)2);
                Console.WriteLine("    LED AZUL - Voltou ao normal");
                break;

            case '4':
                Console.WriteLine(">>> BLOQUEANDO...");
                catraca.Send(Commands.SetFlowControlExtended, (byte)8);
                Console.WriteLine("    LED VERMELHO - Bloqueado permanente!");
                break;

            case '5':
                Console.WriteLine(">>> VOLTANDO AO NORMAL...");
                catraca.Send(Commands.SetFlowControlExtended, (byte)2);
                Console.WriteLine("    LED AZUL - Estado normal/controlado");
                break;

            case '0':
                Console.WriteLine("Encerrando...");
                catraca.Send(Commands.SetFlowControlExtended, (byte)2); // Garantir estado normal
                catraca.Close();
                return;

            default:
                Console.WriteLine("Opção inválida!");
                break;
        }

        Console.WriteLine();
        Console.WriteLine("Pressione qualquer tecla para continuar...");
        Console.ReadKey();
        Console.Clear();
        Console.WriteLine("╔════════════════════════════════════════════════════════╗");
        Console.WriteLine("║      DEMO - INTEGRAÇÃO CATRACA TOLETUS LITENET2        ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════╝");
        Console.WriteLine();
    }
}
catch (Exception ex)
{
    Console.WriteLine($"ERRO: {ex.Message}");
    Console.WriteLine();
    Console.WriteLine("Pressione qualquer tecla para sair...");
    Console.ReadKey();
}
