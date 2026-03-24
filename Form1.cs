using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;

namespace Editordetexto
{
    public partial class Form1 : Form
    {
        // consumible por NextToken
        private List<string> Identificadores; // almacena nombres reales de identificadores en el orden emitido
        private int idConsumeIndex;           // índice para consumir Identificadores desde NextToken
        private string lastIdentifierName;    // nombre real del ultimo identificador devuelto por NextToken
        private bool hasInclude;              // true si se detectó include/define válido
        private bool hasMain;                 // true si se detectó función main

        // Tabla de símbolos
        private SymbolTableManager symbolTable;
        private string currentType; // tipo actual durante declaraciones
        private string currentFunctionName; // nombre de la función que se está procesando


        public Form1()
        {
            InitializeComponent();
            compilarSoluciónToolStripMenuItem.Enabled = false;
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            P_Reservadas = new List<string>
            {
                "void","int","float","double","char","short","long",
                "signed","unsigned","const","volatile",
                "if","else","switch","case","default",
                "for","while","do","break","continue","return",
                "struct","union","enum","sizeof",
                "define","include"
            };

            Identificadores = new List<string>();
        }

        // =========================================================
        // MENÚ / ACCIONES BÁSICAS
        // =========================================================

        private void abrirToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog VentanaAbrir = new OpenFileDialog();
            VentanaAbrir.Filter = "Texto|*.c";
            if (VentanaAbrir.ShowDialog() == DialogResult.OK)
            {
                archivo = VentanaAbrir.FileName;
                using (StreamReader LeerF = new StreamReader(archivo))
                {
                    CajaTxt1.Text = LeerF.ReadToEnd();
                }
            }
            this.Text = "Mi Compilador - " + archivo;
            compilarSoluciónToolStripMenuItem.Enabled = true;
        }

        private void guardar()
        {
            SaveFileDialog VentanaGuardar = new SaveFileDialog();
            VentanaGuardar.Filter = "Texto|*.c";
            if (archivo != null)
            {
                using (StreamWriter EscribirF = new StreamWriter(archivo))
                {
                    EscribirF.Write(CajaTxt1.Text);
                }
            }
            else
            {
                if (VentanaGuardar.ShowDialog() == DialogResult.OK)
                {
                    archivo = VentanaGuardar.FileName;
                    using (StreamWriter EscribirF = new StreamWriter(archivo))
                    {
                        EscribirF.Write(CajaTxt1.Text);
                    }
                }
            }
            this.Text = "Mi Compilador - " + archivo;
        }

        private void gurdarToolStripMenuItem_Click(object sender, EventArgs e)
        {
            guardar();
        }

        private void nuevoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CajaTxt1.Clear();
            archivo = null;
        }

        private void guardarComoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog VentanaGuardar = new SaveFileDialog();
            VentanaGuardar.Filter = "Texto|*.c";
            if (VentanaGuardar.ShowDialog() == DialogResult.OK)
            {
                archivo = VentanaGuardar.FileName;
                using (StreamWriter EscribirF = new StreamWriter(archivo))
                {
                    EscribirF.Write(CajaTxt1.Text);
                }
            }
            this.Text = "Mi Compilador - " + archivo;
        }

        private void salirToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void compilarSoluciónToolStripMenuItem_Click(object sender, EventArgs e)
        {
            TxtboxSalida.Clear();
            if (archivo == null) guardar();
            else guardar();

            Numero_linea = 1;
            N_error = 0;
            elemento = "";

            archivoback = archivo.Remove(archivo.Length - 1) + "back";

            try
            {
                AnalizadorLexico();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error crítico en el compilador: " + ex.Message);
            }

            if (N_error == 0)
            {
                TxtboxSalida.AppendText("\r\nCompilación Exitosa. 0 Errores.\r\n");
            }
            else
            {
                TxtboxSalida.AppendText($"\r\nCompilación Finalizada con {N_error} errores.\r\n");
            }
        }

        private void TxtboxSalida_TextChanged(object sender, EventArgs e)
        {
            compilarSoluciónToolStripMenuItem.Enabled = true;
        }

        private void compilarToolStripMenuItem_Click(object sender, EventArgs e)
        {
        }

        private void CajaTxt1_TextChanged(object sender, EventArgs e)
        {
            compilarSoluciónToolStripMenuItem.Enabled = true;
        }

        // =========================================================
        // ANALIZADOR LÉXICO
        // =========================================================

        private char Tipo_caracter(int caracter)
        {
            if ((caracter >= 65 && caracter <= 90) || (caracter >= 97 && caracter <= 122) || caracter == 95)
                return 'l';
            else if (caracter >= 48 && caracter <= 57)
                return 'd';
            else
            {
                switch (caracter)
                {
                    case 10: return 'n';
                    case 34: return '"';
                    case 39: return 'c';
                    case 32: return 'e';
                    case 13: return 'e';
                    case 9: return 'e';
                    default: return 's';
                }
            }
        }

        private void Simbolo()
        {
            string s = ((char)i_caracter).ToString();
            int siguiente = Leer.Peek();

            if (s == "=" && siguiente == 61) { s = "=="; Leer.Read(); }
            else if (s == "!" && siguiente == 61) { s = "!="; Leer.Read(); }
            else if (s == "<" && siguiente == 61) { s = "<="; Leer.Read(); }
            else if (s == ">" && siguiente == 61) { s = ">="; Leer.Read(); }
            else if (s == "+" && siguiente == 43) { s = "++"; Leer.Read(); }
            else if (s == "-" && siguiente == 45) { s = "--"; Leer.Read(); }
            else if (s == "&" && siguiente == 38) { s = "&&"; Leer.Read(); }
            else if (s == "|" && siguiente == 124) { s = "||"; Leer.Read(); }
            else if ((s == "+" || s == "-" || s == "*" || s == "/" || s == "%") && siguiente == 61)
            {
                s = s + "=";
                Leer.Read();
            }

            if ("(){}[],;=+-*/%<>!&|#:".Contains(((char)i_caracter).ToString()) || s.Length > 1)
            {
                Escribir.Write(s + "\n");
            }
            else
            {
                ErrorLexico($"Símbolo desconocido '{s}'");
            }
        }

        private void Cadena()
        {
            i_caracter = Leer.Read();

            while (i_caracter != -1 && (char)i_caracter != '"')
            {
                char c = (char)i_caracter;

                if (c == 10 || c == 13)
                {
                    ErrorLexico("Cadena sin cerrar (Salto de línea encontrado).");
                    Escribir.Write("Cadena\n");
                    return;
                }

                i_caracter = Leer.Read();
            }

            if (i_caracter == -1)
            {
                ErrorLexico("Cadena sin cerrar (Fin de archivo).");
                Escribir.Write("Cadena\n");
                return;
            }

            Escribir.Write("Cadena\n");
            i_caracter = Leer.Read();
        }

        private void Caracter()
        {
            i_caracter = Leer.Read();

            if (i_caracter == -1 || i_caracter == 39)
            {
                ErrorLexico("Carácter vacío o incompleto");
                if (i_caracter == 39) i_caracter = Leer.Read();
                return;
            }

            int cierre = Leer.Read();
            if (cierre != 39)
            {
                ErrorLexico("Se esperaba comilla simple de cierre");
                i_caracter = cierre;
                return;
            }

            Escribir.Write("caracter\n");
            i_caracter = Leer.Read();
        }

        private void Archivo_Libreria()
        {
            elemento += ".";
            i_caracter = Leer.Read();

            while (Tipo_caracter(i_caracter) == 'l')
            {
                elemento += (char)i_caracter;
                i_caracter = Leer.Read();
            }

            Escribir.Write("libreria\n");
        }

        private bool Palabra_Reservada()
        {
            return P_Reservadas.IndexOf(elemento.ToLower()) >= 0;
        }

        private void Identificador()
        {
            do
            {
                elemento += (char)i_caracter;
                i_caracter = Leer.Read();
            } while (Tipo_caracter(i_caracter) == 'l' || Tipo_caracter(i_caracter) == 'd');

            if ((char)i_caracter == '.')
            {
                Archivo_Libreria();
            }
            else
            {
                if (Palabra_Reservada())
                    Escribir.Write(elemento.ToLower() + "\n");
                else
                {
                    Identificadores.Add(elemento);
                    Escribir.Write("identificador\n");
                }
            }
        }

        private void Numero()
        {
            do
            {
                i_caracter = Leer.Read();
            } while (Tipo_caracter(i_caracter) == 'd');

            if ((char)i_caracter == '.')
            {
                Numero_Real();
                return;
            }

            Escribir.Write("numero_entero\n");
        }

        private void Numero_Real()
        {
            i_caracter = Leer.Read();

            while (Tipo_caracter(i_caracter) == 'd')
            {
                i_caracter = Leer.Read();
            }

            Escribir.Write("numero_real\n");
        }

        private bool Comentario()
        {
            int siguiente = Leer.Read();

            if (siguiente == 47)
            {
                do { i_caracter = Leer.Read(); } while (i_caracter != 10 && i_caracter != -1);
                return true;
            }
            else if (siguiente == 42)
            {
                bool cerrado = false;
                i_caracter = Leer.Read();
                do
                {
                    if (i_caracter == 10)
                    {
                        Numero_linea++;
                        Escribir.Write("LF\n");
                    }

                    if (i_caracter == 42)
                    {
                        if (Leer.Peek() == 47)
                        {
                            Leer.Read();
                            cerrado = true;
                            break;
                        }
                    }
                    i_caracter = Leer.Read();
                } while (i_caracter != -1);

                if (!cerrado) ErrorLexico("Comentario de bloque sin cerrar");

                i_caracter = Leer.Read();
                return true;
            }
            else
            {
                Escribir.Write("/\n");
                i_caracter = siguiente;
                return true;
            }
        }

        private void Error(int i_caracter)
        {
            TxtboxSalida.AppendText($"Error léxico '{(char)i_caracter}', línea {Numero_linea}\n");
            N_error++;
        }

        private void Error(string mensaje)
        {
            TxtboxSalida.AppendText($"Error sintáctico: {mensaje}, línea {linea_del_token}\n");
            N_error++;
        }

        private void Error(string tokenLocal, string esperado)
        {
            TxtboxSalida.AppendText($"Error: se esperaba '{esperado}', pero se encontró '{tokenLocal}', línea {linea_del_token}\n");
            N_error++;
        }

        private void ErrorLexico(string msg)
        {
            TxtboxSalida.AppendText($"Error léxico: {msg}, línea {Numero_linea}\n");
            N_error++;
        }

        private string NextToken()
        {
            string t = Leer.ReadLine();
            while (t == "LF")
            {
                Numero_linea++;
                t = Leer.ReadLine();
            }

            linea_del_token = Numero_linea;

            if (t == "identificador")
            {
                if (Identificadores != null && idConsumeIndex < Identificadores.Count)
                    lastIdentifierName = Identificadores[idConsumeIndex++];
                else
                    lastIdentifierName = null;
            }
            else
            {
                lastIdentifierName = null;
            }

            return t;
        }

        private void AnalizadorLexico()
        {
            Numero_linea = 1;
            N_error = 0;

            Identificadores.Clear();
            idConsumeIndex = 0;
            lastIdentifierName = null;
            hasInclude = false;
            hasMain = false;

            Leer = new StreamReader(archivo);
            string archivoSalida = archivo.Remove(archivo.Length - 1) + "back";
            Escribir = new StreamWriter(archivoSalida);

            i_caracter = Leer.Read();

            while (i_caracter != -1)
            {
                elemento = "";

                if ((char)i_caracter == '/')
                {
                    if (Comentario())
                    {
                        continue;
                    }
                }

                switch (Tipo_caracter(i_caracter))
                {
                    case 'l':
                        Identificador();
                        break;

                    case 'd':
                        Numero();
                        break;

                    case '"':
                        Cadena();
                        break;

                    case 'c':
                        Caracter();
                        break;

                    case 'n':
                        Numero_linea++;
                        Escribir.Write("LF\n");
                        i_caracter = Leer.Read();
                        break;

                    case 'e':
                        i_caracter = Leer.Read();
                        break;

                    case 's':
                        Simbolo();
                        i_caracter = Leer.Read();
                        break;

                    default:
                        Error(i_caracter);
                        i_caracter = Leer.Read();
                        break;
                }
            }

            Escribir.Write("Fin\n");
            Escribir.Close();
            Leer.Close();

            AnalizadorSintactico();

            TxtboxSalida.AppendText($"\nProceso finalizado. Errores: {N_error}\n");
        }

        // =========================================================
        // ANALIZADOR SINTÁCTICO
        // =========================================================

        private void AnalizadorSintactico()
        {
            Numero_linea = 1;
            Leer = new StreamReader(archivoback);

            idConsumeIndex = 0;
            lastIdentifierName = null;
            hasInclude = false;
            hasMain = false;

            symbolTable = new SymbolTableManager();
            symbolTable.Reset();
            symbolTable.AddBuiltinFunction("printf", "int");

            token = NextToken();
            Cabecera();

            Leer.Close();

            if (!hasInclude)
            {
                TxtboxSalida.AppendText("\r\nSugerencia: No se detectó ninguna cabecera (#include o #define). Añade una directiva #include o #define si es necesario (ej: #include <stdio.h>).\r\n");
            }
            if (!hasMain)
            {
                TxtboxSalida.AppendText("\r\nSugerencia: No se detectó la función 'main'. Añade la función principal (ej: int main() { /*...*/ }) para el punto de entrada.\r\n");
            }
        }

        private bool EsTipoDato(string t)
        {
            return t == "int" || t == "float" || t == "double" || t == "char" || t == "void" || t == "Tipo";
        }

        private void Cabecera()
        {
            while (token != null && token != "Fin")
            {
                switch (token)
                {
                    case "#":
                        token = NextToken();
                        if (token == null)
                        {
                            Error("Directiva incompleta después de '#'");
                            return;
                        }
                        Directiva_proc();
                        break;

                    case "LF":
                        token = NextToken();
                        break;

                    case "int":
                    case "float":
                    case "double":
                    case "char":
                    case "void":
                    case "Tipo":
                        ProcesarDeclaracionOFuncion();
                        break;

                    default:
                        token = NextToken();
                        break;
                }
            }
        }

        private void ProcesarDeclaracionOFuncion()
        {
            string tipo = token;
            currentType = tipo;

            token = NextToken();

            if (token == null)
            {
                Error("Declaración incompleta");
                return;
            }

            if (token != "identificador")
            {
                Error("Se esperaba un identificador después del tipo de dato");
                return;
            }

            string idToken = token;
            string idNombreReal = lastIdentifierName;

            token = NextToken();

            if (token == "(")
            {
                if (!string.IsNullOrEmpty(idNombreReal) && idNombreReal == "main")
                    hasMain = true;

                string funcName = idNombreReal ?? idToken;

                if (!string.IsNullOrEmpty(funcName))
                {
                    if (!symbolTable.AddFunction(funcName, tipo, out string err))
                        Error(err);
                }

                currentFunctionName = funcName;

                // Ámbito de parámetros / función
                symbolTable.EnterScope();

                Parametros();
                BloqueDeSentencias();

                symbolTable.ExitScope();
                currentFunctionName = null;

                token = NextToken();
            }
            else
            {
                Declaracion_Variable_Global_Logica(idToken, idNombreReal);
                token = NextToken();
            }
        }

        private void Parametros()
        {
            token = NextToken();
            if (token == ")")
            {
                token = NextToken();
                return;
            }

            while (token != ")" && token != "Fin")
            {
                if (!EsTipoDato(token) || token == "Tipo")
                    Error(token, "tipo de dato");

                string tipoParametro = token;
                token = NextToken();

                if (token != "identificador")
                {
                    Error(token, "identificador");
                    token = NextToken();
                    if (token == ")") break;
                }
                else
                {
                    string nombreParametro = lastIdentifierName;

                    if (!string.IsNullOrEmpty(nombreParametro))
                    {
                        if (!symbolTable.AddVariable(nombreParametro, tipoParametro, out string errVar))
                        {
                            Error(errVar);
                        }
                    }

                    if (!string.IsNullOrEmpty(currentFunctionName))
                    {
                        var f = symbolTable.GetFunction(currentFunctionName);
                        if (f != null)
                        {
                            f.TiposParametros.Add(tipoParametro);
                        }
                    }

                    token = NextToken();

                    while (token == "[")
                    {
                        token = NextToken();
                        if (token != "numero_entero" && token != "identificador")
                        {
                            Error(token, "tamaño arreglo");
                            return;
                        }
                        token = NextToken();
                        if (token != "]") { Error(token, "]"); return; }
                        token = NextToken();
                    }
                }

                if (token == ",") token = NextToken();
                else if (token != ")") { Error(token, "',' o ')'"); return; }
            }

            token = NextToken();
        }

        private void BloqueDeSentencias()
        {
            if (token != "{") { Error(token, "{"); return; }

            symbolTable.EnterScope();
            token = NextToken();

            while (token != "}" && token != "Fin" && token != null)
            {
                switch (token)
                {
                    case "int":
                    case "float":
                    case "double":
                    case "char":
                        Declaracion_Local();
                        break;

                    case "if":
                        EstructuraIf();
                        break;

                    case "while":
                        EstructuraWhile();
                        break;

                    case "do":
                        EstructuraDoWhile();
                        break;

                    case "for":
                        EstructuraFor();
                        break;

                    case "switch":
                        EstructuraSwitch();
                        break;

                    case "break":
                    case "continue":
                        token = NextToken();
                        if (token != ";") Error(token, ";");
                        token = NextToken();
                        break;

                    case "return":
                        token = NextToken();

                        if (token != ";")
                        {
                            AnalizarExpresion();
                        }

                        if (token != ";") Error(token, ";");
                        token = NextToken();
                        break;

                    case "identificador":
                        Sentencia();
                        break;

                    case ";":
                        token = NextToken();
                        break;

                    case "{":
                        BloqueDeSentencias();
                        token = NextToken();
                        break;

                    case "++":
                    case "--":
                        {
                            token = NextToken();
                            if (token != "identificador")
                            {
                                Error(token, "identificador");
                                break;
                            }

                            if (!symbolTable.VariableDeclared(lastIdentifierName) && !symbolTable.FunctionExists(lastIdentifierName))
                            {
                                Error($"La variable '{lastIdentifierName}' no ha sido declarada");
                            }

                            token = NextToken();
                            if (token != ";") Error(token, ";");
                            token = NextToken();
                        }
                        break;

                    default:
                        Error($"Instrucción no reconocida o inválida: '{token}'");
                        token = NextToken();
                        break;
                }
            }

            if (token != "}") Error("Se esperaba '}'");

            symbolTable.ExitScope();
        }

        private void SentenciaOBloque()
        {
            if (token == "{")
            {
                BloqueDeSentencias();
                token = NextToken();
                return;
            }

            if (token == "identificador")
            {
                Sentencia();
                return;
            }

            if (token == "break" || token == "continue")
            {
                token = NextToken();
                if (token != ";") Error(token, ";");
                token = NextToken();
                return;
            }

            if (token == ";")
            {
                token = NextToken();
                return;
            }

            Error(token, "sentencia o '{'");
            token = NextToken();
        }

        private void Sentencia()
        {
            string id = token;
            string nameId = lastIdentifierName;

            if (id == "identificador")
            {
                if (!symbolTable.VariableDeclared(nameId) && !symbolTable.FunctionExists(nameId))
                {
                    Error($"La variable o función '{nameId}' no ha sido declarada");
                }
            }

            token = NextToken();

            if (token == "(")
            {
                token = NextToken();
                if (token != ")")
                {
                    while (true)
                    {
                        AnalizarExpresion();
                        if (token == ",") { token = NextToken(); continue; }
                        else if (token == ")") break;
                        else { Error(token, ", o )"); return; }
                    }
                }

                token = NextToken();
                if (token != ";") Error(token, ";");
                token = NextToken();
                return;
            }

            while (token == "[")
            {
                token = NextToken();
                AnalizarExpresion();
                if (token != "]") { Error(token, "]"); return; }
                token = NextToken();
            }

            if (token == "++" || token == "--")
            {
                token = NextToken();
                if (token != ";") Error(token, ";");
                token = NextToken();
                return;
            }

            if (token == "=" || token == "+=" || token == "-=" || token == "*=" || token == "/=" || token == "%=")
            {
                token = NextToken();
                AnalizarExpresion();
                if (token != ";") Error(token, ";");
                token = NextToken();
                return;
            }

            if (token == ";")
            {
                token = NextToken();
                return;
            }

            Error(token, "'=' o '(' o '++/--' o '[' (indexación)");
            token = NextToken();
        }

        // =========================================================
        // ESTRUCTURAS DE CONTROL
        // =========================================================

        private void EstructuraIf()
        {
            token = NextToken();
            if (token != "(") { Error(token, "("); return; }
            token = NextToken();
            AnalizarExpresion();
            if (token != ")") { Error(token, ")"); return; }
            token = NextToken();

            SentenciaOBloque();

            if (token == "else")
            {
                token = NextToken();
                SentenciaOBloque();
            }
        }

        private void EstructuraWhile()
        {
            token = NextToken();
            if (token != "(") { Error(token, "("); return; }
            token = NextToken();
            AnalizarExpresion();
            if (token != ")") { Error(token, ")"); return; }
            token = NextToken();

            SentenciaOBloque();
        }

        private void EstructuraFor()
        {
            token = NextToken();
            if (token != "(") { Error(token, "("); return; }
            token = NextToken();

            if (EsTipoDato(token) && token != "void")
            {
                Declaracion_Local();
            }
            else if (token == "identificador")
            {
                Sentencia();
            }
            else if (token == ";")
            {
                token = NextToken();
            }
            else
            {
                Error(token, "inicialización for");
                token = NextToken();
            }

            if (token != ";")
            {
                AnalizarExpresion();
            }
            if (token != ";") { Error(token, ";"); return; }
            token = NextToken();

            if (token != ")")
            {
                if (token == "identificador")
                {
                    token = NextToken();
                    if (token == "=")
                    {
                        token = NextToken();
                        AnalizarExpresion();
                    }
                    else
                    {
                        if (token == "++" || token == "--")
                        {
                            token = NextToken();
                        }
                        else
                        {
                            AnalizarExpresion();
                        }
                    }
                }
                else
                {
                    AnalizarExpresion();
                }
            }

            if (token != ")") { Error(token, ")"); return; }
            token = NextToken();

            SentenciaOBloque();
        }

        private void EstructuraDoWhile()
        {
            token = NextToken();

            SentenciaOBloque();

            if (token != "while") { Error(token, "while"); return; }
            token = NextToken();
            if (token != "(") { Error(token, "("); return; }
            token = NextToken();
            AnalizarExpresion();
            if (token != ")") { Error(token, ")"); return; }
            token = NextToken();
            if (token != ";") { Error(token, ";"); return; }
            token = NextToken();
        }

        private void EstructuraSwitch()
        {
            token = NextToken();
            if (token != "(") { Error(token, "("); return; }
            token = NextToken();
            AnalizarExpresion();
            if (token != ")") { Error(token, ")"); return; }
            token = NextToken();
            if (token != "{") { Error(token, "{"); return; }
            token = NextToken();

            while (token != "}" && token != "Fin" && token != null)
            {
                if (token == "case")
                {
                    token = NextToken();
                    if (token != "numero_entero" && token != "caracter") Error(token, "constante");
                    token = NextToken();
                    if (token != ":") { Error(token, ":"); return; }
                    token = NextToken();
                    CuerpoDelCase();
                }
                else if (token == "default")
                {
                    token = NextToken();
                    if (token != ":") { Error(token, ":"); return; }
                    token = NextToken();
                    CuerpoDelCase();
                }
                else
                {
                    Error($"Se esperaba 'case' o 'default', pero se encontró '{token}'");
                    token = NextToken();
                }
            }

            if (token == "}") token = NextToken();
        }

        private void CuerpoDelCase()
        {
            while (token != "case" && token != "default" && token != "}" && token != "Fin")
            {
                if (token == "break")
                {
                    token = NextToken();
                    if (token != ";") Error(token, ";");
                    token = NextToken();
                }
                else if (token == "identificador")
                {
                    Sentencia();
                }
                else if (token == "if")
                {
                    EstructuraIf();
                }
                else if (token == "while")
                {
                    EstructuraWhile();
                }
                else if (token == "for")
                {
                    EstructuraFor();
                }
                else if (token == "{")
                {
                    BloqueDeSentencias();
                    token = NextToken();
                }
                else
                {
                    token = NextToken();
                }
            }
        }

        // =========================================================
        // ÁRBOL DE EXPRESIONES / ANÁLISIS SINTÁCTICO DE EXPRESIONES
        // =========================================================

        private void ImprimirArbol(NodoExpresion nodo, string prefijo = "", bool esRaiz = true)
        {
            if (nodo == null) return;

            if (esRaiz)
                TxtboxSalida.AppendText($"Árbol: {nodo.Valor}\r\n");
            else
                TxtboxSalida.AppendText($"{prefijo}└── {nodo.Valor}\r\n");

            string nuevoPrefijo = esRaiz ? "    " : prefijo + "    ";
            ImprimirArbol(nodo.Izquierdo, nuevoPrefijo, false);
            ImprimirArbol(nodo.Derecho, nuevoPrefijo, false);
        }

        private bool EsInicioOperando()
        {
            return token == "identificador" ||
                   token == "numero_entero" ||
                   token == "numero_real" ||
                   token == "caracter" ||
                   token == "Cadena" ||
                   token == "(" ||
                   token == "++" ||
                   token == "--" ||
                   token == "!" ||
                   token == "+" ||
                   token == "-";
        }

        private bool EsOperadorBinario()
        {
            return token == "+" || token == "-" ||
                   token == "*" || token == "/" || token == "%" ||
                   token == "==" || token == "!=" ||
                   token == "<" || token == ">" ||
                   token == "<=" || token == ">=" ||
                   token == "&&" || token == "||";
        }

        private NodoExpresion AnalizarExpresion()
        {
            if (!EsInicioOperando())
            {
                Error($"Se esperaba una expresión, se encontró '{token}'");
                return new NodoExpresion("?");
            }

            NodoExpresion raiz = Expresion();
            
            return raiz;
        }

        private NodoExpresion Expresion()
        {
            return Condicion();
        }

        private NodoExpresion Condicion()
        {
            NodoExpresion izq = TerminoAND();

            while (token == "||")
            {
                string op = token;
                token = NextToken();
                NodoExpresion der = TerminoAND();
                izq = new NodoExpresion(op, izq, der);
            }

            return izq;
        }

        private NodoExpresion TerminoAND()
        {
            NodoExpresion izq = ExpresionIgualdad();

            while (token == "&&")
            {
                string op = token;
                token = NextToken();
                NodoExpresion der = ExpresionIgualdad();
                izq = new NodoExpresion(op, izq, der);
            }

            return izq;
        }

        private NodoExpresion ExpresionIgualdad()
        {
            NodoExpresion izq = ExpresionRelacional();

            while (token == "==" || token == "!=")
            {
                string op = token;
                token = NextToken();
                NodoExpresion der = ExpresionRelacional();
                izq = new NodoExpresion(op, izq, der);
            }

            return izq;
        }

        private NodoExpresion ExpresionRelacional()
        {
            NodoExpresion izq = ExpresionAritmetica();

            while (token == "<" || token == ">" || token == "<=" || token == ">=")
            {
                string op = token;
                token = NextToken();
                NodoExpresion der = ExpresionAritmetica();
                izq = new NodoExpresion(op, izq, der);
            }

            return izq;
        }

        private NodoExpresion ExpresionAritmetica()
        {
            NodoExpresion izq = Termino();

            while (token == "+" || token == "-")
            {
                string op = token;
                token = NextToken();
                NodoExpresion der = Termino();
                izq = new NodoExpresion(op, izq, der);
            }

            return izq;
        }

        private NodoExpresion Termino()
        {
            NodoExpresion izq = Factor();

            while (token == "*" || token == "/" || token == "%")
            {
                string op = token;
                token = NextToken();
                NodoExpresion der = Factor();
                izq = new NodoExpresion(op, izq, der);
            }

            return izq;
        }

        private NodoExpresion Factor()
        {
            // Prefijo unario
            if (token == "++" || token == "--" || token == "!" || token == "+" || token == "-")
            {
                string op = token;
                token = NextToken();
                NodoExpresion der = Factor();
                return new NodoExpresion(op, der, null);
            }

            // Paréntesis o cast
            if (token == "(")
            {
                token = NextToken();

                if (EsTipoDato(token))
                {
                    string tipoCast = token;
                    token = NextToken();

                    if (token != ")")
                    {
                        Error(token, ")");
                        return new NodoExpresion("?");
                    }

                    token = NextToken();
                    NodoExpresion ex = Factor();
                    return new NodoExpresion($"cast:{tipoCast}", ex, null);
                }
                else
                {
                    NodoExpresion ex = Expresion();

                    if (token != ")")
                    {
                        Error("Falta ')' de cierre en la expresión");
                        return new NodoExpresion("?");
                    }

                    token = NextToken();
                    return new NodoExpresion("()", ex, null);
                }
            }

            // Literal entero
            if (token == "numero_entero")
            {
                NodoExpresion n = new NodoExpresion("numero_entero");
                token = NextToken();
                return n;
            }

            // Literal real
            if (token == "numero_real")
            {
                NodoExpresion n = new NodoExpresion("numero_real");
                token = NextToken();
                return n;
            }

            // Literal carácter
            if (token == "caracter")
            {
                NodoExpresion n = new NodoExpresion("caracter");
                token = NextToken();
                return n;
            }

            // Cadena
            if (token == "Cadena")
            {
                NodoExpresion n = new NodoExpresion("Cadena");
                token = NextToken();
                return n;
            }

            // Identificador
            if (token == "identificador")
            {
                string nombre = lastIdentifierName;

                if (!string.IsNullOrEmpty(nombre))
                {
                    if (!symbolTable.VariableDeclared(nombre) && !symbolTable.FunctionExists(nombre))
                    {
                        Error($"La variable o función '{nombre}' no ha sido declarada");
                    }
                }

                token = NextToken();

                // Llamada a función
                if (token == "(")
                {
                    if (!symbolTable.FunctionExists(nombre))
                    {
                        Error($"La función '{nombre}' no ha sido declarada");
                    }

                    NodoExpresion nodoLlamada = new NodoExpresion($"func:{nombre}");
                    token = NextToken();

                    if (token != ")")
                    {
                        nodoLlamada.Izquierdo = Expresion();
                        NodoExpresion actual = nodoLlamada;

                        while (token == ",")
                        {
                            token = NextToken();
                            NodoExpresion argExtra = Expresion();
                            actual.Derecho = new NodoExpresion(",", argExtra, null);
                            actual = actual.Derecho;
                        }
                    }

                    if (token != ")")
                        Error("Falta ')' al cerrar la llamada a función");
                    else
                        token = NextToken();

                    return nodoLlamada;
                }

                // Indexación
                while (token == "[")
                {
                    token = NextToken();
                    NodoExpresion indice = Expresion();

                    if (token != "]")
                    {
                        Error("Falta ']' en la indexación");
                        return new NodoExpresion(nombre);
                    }

                    token = NextToken();
                    nombre = $"{nombre}[idx]";
                }

                // Postfijo
                if (token == "++" || token == "--")
                {
                    string opPost = token;
                    if (!string.IsNullOrEmpty(nombre) && !symbolTable.VariableDeclared(lastIdentifierName))
                        Error($"La variable '{lastIdentifierName}' no ha sido declarada");

                    token = NextToken();
                    return new NodoExpresion(nombre + opPost);
                }

                return new NodoExpresion(nombre);
            }

            Error($"Se esperaba un operando, se encontró '{token}'");
            token = NextToken();
            return new NodoExpresion("?");
        }

        // =========================================================
        // AUXILIARES Y DECLARACIONES
        // =========================================================

        private void Declaracion_Local()
        {
            currentType = token;

            token = NextToken();
            string idToken = token;

            string nombreIdent = null;
            if (idToken == "identificador")
                nombreIdent = lastIdentifierName;

            token = NextToken();

            Declaracion_Variable_Global_Logica(idToken, nombreIdent);

            token = NextToken();
        }

        private void Declaracion_Variable_Global_Logica(string identificador_actual, string nombreReal = null)
        {
            void ProcesarDeclarador()
            {
                while (token == "[")
                {
                    token = NextToken();
                    if (token != "numero_entero" && token != "identificador")
                    {
                        Error(token, "tamaño arreglo");
                        return;
                    }
                    token = NextToken();
                    if (token != "]") { Error(token, "]"); return; }
                    token = NextToken();
                }

                if (token == "=")
                {
                    token = NextToken();

                    if (token == "{")
                    {
                        BloqueInicializacion();
                    }
                    else
                    {
                        AnalizarExpresion();
                    }
                }
            }

            if (identificador_actual == "identificador")
            {
                string nombre = nombreReal ?? lastIdentifierName;
                if (!string.IsNullOrEmpty(nombre))
                {
                    if (!symbolTable.AddVariable(nombre, currentType, out string errAdd))
                    {
                        Error(errAdd);
                    }
                }
                else
                {
                    Error("Identificador sin nombre disponible para la declaración");
                }
            }

            ProcesarDeclarador();

            while (token == ",")
            {
                token = NextToken();
                if (token != "identificador") { Error(token, "identificador"); return; }

                string nombre2 = lastIdentifierName;
                token = NextToken();

                if (!string.IsNullOrEmpty(nombre2))
                {
                    if (!symbolTable.AddVariable(nombre2, currentType, out string errAdd2))
                    {
                        Error(errAdd2);
                    }
                }
                else
                {
                    Error("Identificador sin nombre disponible en lista.");
                }

                ProcesarDeclarador();
            }

            if (token != ";") Error(token, ";");
        }

        private void BloqueInicializacion()
        {
            if (token != "{") { Error(token, "{"); return; }
            token = NextToken();

            while (token != "}")
            {
                if (token == "{") BloqueInicializacion();
                else if (token == "numero_entero" || token == "numero_real" || token == "identificador" || token == "Cadena" || token == "caracter")
                {
                    token = NextToken();
                }
                else
                {
                    Error(token, "valor o sub-arreglo");
                    return;
                }

                if (token == ",") token = NextToken();
                else if (token == "}") break;
                else { Error(token, "',' o '}'"); return; }
            }

            token = NextToken();
        }

        private int Directiva_proc()
        {
            if (token == "include")
            {
                token = NextToken();
                while (token == "LF") token = NextToken();

                if (token == null || token == "Fin")
                {
                    Error("Include incompleto");
                    return 0;
                }

                return Directiva_include();
            }
            else if (token == "define")
            {
                hasInclude = true;
                token = NextToken();

                while (token != null && token != "Fin" && token != "LF")
                    token = NextToken();

                return 1;
            }

            Error("include o define");
            return 0;
        }

        private int Directiva_include()
        {
            while (token == "LF")
            {
                Numero_linea++;
                token = NextToken();
            }

            if (token == null) return 0;

            if (token == "<")
            {
                token = NextToken();
                bool tieneContenido = false;

                while (token != ">" && token != "Fin" && token != "LF" && token != null)
                {
                    if (token != "libreria")
                        tieneContenido = true;

                    token = NextToken();
                }

                if (token != ">")
                {
                    Error(token, ">");
                    return 0;
                }

                if (!tieneContenido)
                    hasInclude = true;
                else
                    hasInclude = true;

                token = NextToken();
                return 1;
            }
            else if (token == "Cadena")
            {
                hasInclude = true;
                token = NextToken();
                return 1;
            }

            Error("Formato include");
            return 0;
        }
    }
}