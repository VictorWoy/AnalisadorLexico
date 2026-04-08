using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace GeradorTokens
{
    //  Tipos de token
    public enum TipoToken
    {
        RESERVADA,
        IDENT,
        INT_LIT,
        FLOAT_LIT,
        OP,
        DELIM,
        COMENTARIO,
        INVALIDO
    }

    //  Entrada da tabela de símbolos
    public class EntradaSimbolo
    {
        public int Indice { get; }
        public string Elemento { get; }
        public TipoToken Tipo { get; }
        public int Linha { get; }

        public EntradaSimbolo(int indice, string elemento, TipoToken tipo, int linha)
        {
            Indice = indice;
            Elemento = elemento;
            Tipo = tipo;
            Linha = linha;
        }
    }

    //  Token
    public class Token
    {
        public TipoToken Tipo { get; }
        public string Elemento { get; }
        public EntradaSimbolo? Simbolo { get; }  
        public int Linha { get; }
        public int Coluna { get; }

        public Token(TipoToken tipo, string elemento,
                     EntradaSimbolo? simbolo, int linha, int coluna)
        {
            Tipo = tipo;
            Elemento = elemento;
            Simbolo = simbolo;
            Linha = linha;
            Coluna = coluna;
        }

        /// Formata o token no estilo clássico de compiladores.
        public string Formatar()
        {
            switch (Tipo)
            {
                case TipoToken.COMENTARIO:
                    return "<comentario>";

                case TipoToken.INVALIDO:
                    return $"<invalido, {Elemento}>";

                case TipoToken.OP:
                case TipoToken.DELIM:
                    return $"<{Elemento}>";

                case TipoToken.IDENT:
                    return $"<id, {Simbolo!.Indice}>";

                case TipoToken.INT_LIT:
                case TipoToken.FLOAT_LIT:
                    return $"<num, {Simbolo!.Indice}>";

                case TipoToken.RESERVADA:
                    return $"<{Elemento}, {Simbolo!.Indice}>";

                default:
                    return $"<{Elemento}>";
            }
        }
    }

    public class Analisador
    {
        // Palavras reservadas da linguagem
        private static readonly HashSet<string> Reservadas =
            new(StringComparer.Ordinal)
            { "int", "double", "char", "float", "if", "while", "for" };

        // Regex que define como vai ser a leitura dos Tokens
        private static readonly Regex PatternTokens = new Regex(
            @"(?<comentario>\#[^\n]*)" +
            @"|(?<float_lit>\d+,\d+)" +
            @"|(?<int_lit>\d+)" +
            @"|(?<ident>[A-Za-z]+)" +
            @"|(?<op>==|!=|<=|>=|[+\-*/=<>])" +
            @"|(?<delim>[(){};,])" +
            @"|(?<invalido>\S)",
            RegexOptions.Compiled
        );

        // Estado interno
        private readonly List<Token> _tokens = new();
        private readonly Dictionary<string, EntradaSimbolo> _tabelaSimbolos = new();
        private int _proximoIndice = 1;

        //  Método de analise
        public void Analisar(string fonte)
        {
            _tokens.Clear();
            _tabelaSimbolos.Clear();
            _proximoIndice = 1;

            string[] linhas = fonte.Split('\n');

            for (int i = 0; i < linhas.Length; i++)
            {
                int numLinha = i + 1; // base 1

                // Regex.Matches retorna todas as correspondências da linha na ordem em que aparecem.
                MatchCollection matches = PatternTokens.Matches(linhas[i]);

                foreach (Match m in matches)
                {
                    string elemento = m.Value;
                    int coluna = m.Index + 1;
                    TipoToken tipo = Classificar(m, elemento);

                    // Tokens com entrada na tabela de símbolos:
                    // reservadas, identificadores e literais.
                    EntradaSimbolo? simbolo = null;

                    if (tipo is TipoToken.RESERVADA or TipoToken.IDENT
                             or TipoToken.INT_LIT or TipoToken.FLOAT_LIT)
                    {
                        if (!_tabelaSimbolos.TryGetValue(elemento, out simbolo))
                        {
                            simbolo = new EntradaSimbolo(
                                _proximoIndice++, elemento, tipo, numLinha);
                            _tabelaSimbolos[elemento] = simbolo;
                        }
                    }

                    _tokens.Add(new Token(tipo, elemento, simbolo, numLinha, coluna));
                }
            }
        }

        //  Classificação por grupo nomeado
        //  Essa função recebe um objeto do tipo Match e usa de suas variaveis para definir se foi um sucesso com base nos grupos definidos se sim
        //  o atribui para um tipo de token
        private static TipoToken Classificar(Match m, string elemento)
        {
            if (m.Groups["comentario"].Success) return TipoToken.COMENTARIO;
            if (m.Groups["float_lit"].Success) return TipoToken.FLOAT_LIT;
            if (m.Groups["int_lit"].Success) return TipoToken.INT_LIT;
            if (m.Groups["op"].Success) return TipoToken.OP;
            if (m.Groups["delim"].Success) return TipoToken.DELIM;
            if (m.Groups["invalido"].Success) return TipoToken.INVALIDO;

            if (m.Groups["ident"].Success)
            {
                if (Reservadas.Contains(elemento)) return TipoToken.RESERVADA;
                if (char.IsUpper(elemento[0])) return TipoToken.IDENT;
                // Começa com minúscula e não é reservada = inválido
                return TipoToken.INVALIDO;
            }

            return TipoToken.INVALIDO;
        }

        //  Saída em texto
        public string ObterTabelaSimbolos()
        {
            var sb = new StringBuilder();
            sb.AppendLine("TABELA DE SÍMBOLOS");
            sb.AppendLine("==================");
            sb.AppendLine($"{"Índice",-8} {"Elemento",-20} {"Tipo",-12} {"Linha"}");
            sb.AppendLine(new string('-', 50));

            foreach (EntradaSimbolo e in _tabelaSimbolos.Values)
                sb.AppendLine($"{e.Indice,-8} {e.Elemento,-20} {e.Tipo,-12} {e.Linha}");

            sb.AppendLine();
            sb.AppendLine($"Total: {_tabelaSimbolos.Count} entrada(s)");
            return sb.ToString();
        }

        public string ObterListaTokens()
        {
            var sb = new StringBuilder();
            sb.AppendLine("LISTA DE TOKENS");
            sb.AppendLine("===============");

            int linhaAtual = 0;
            foreach (Token t in _tokens)
            {
                if (t.Linha != linhaAtual)
                {
                    if (linhaAtual != 0) sb.AppendLine();
                    linhaAtual = t.Linha;
                }
                sb.Append(t.Formatar() + " ");
            }

            sb.AppendLine();
            sb.AppendLine();
            sb.AppendLine($"Total: {_tokens.Count} token(s)");
            return sb.ToString();
        }

        public IReadOnlyList<Token> Tokens => _tokens;
        public IReadOnlyDictionary<string, EntradaSimbolo> TabelaSimbolos => _tabelaSimbolos;
    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.WriteLine("GERADOR DE TOKENS — Linguagem");
    
            string fonte = "";
    
            if (args.Length > 0)
            {
                string caminho = args[0];
    
                if (!File.Exists(caminho))
                {
                    Console.WriteLine("Arquivo não encontrado.");
                    return;
                }
    
                fonte = File.ReadAllText(caminho);
            }
            else
            {
                Console.WriteLine("Digite o código (encerre com '###'):");
                var sb = new StringBuilder();
                string? linha;
    
                while ((linha = Console.ReadLine()) != null && linha.Trim() != "###")
                    sb.AppendLine(linha);
    
                fonte = sb.ToString();
            }
    
            if (string.IsNullOrWhiteSpace(fonte))
            {
                Console.WriteLine("Nenhum código fornecido.");
                return;
            }
    
            var analisador = new Analisador();
            analisador.Analisar(fonte);
    
            Console.WriteLine();
            Console.WriteLine(analisador.ObterTabelaSimbolos());
            Console.WriteLine(analisador.ObterListaTokens());
        }
    }
}
