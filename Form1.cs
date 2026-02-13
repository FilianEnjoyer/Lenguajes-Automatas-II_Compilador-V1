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
        }
        private void abrirToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog VentanaAbrir = new OpenFileDialog();
            VentanaAbrir.Filter = "Texto|*.c";
            if (VentanaAbrir.ShowDialog() == DialogResult.OK)
            {
                archivo = VentanaAbrir.FileName;
                using (StreamReader Leer = new StreamReader(archivo))
                {
                    CajaTxt1.Text = Leer.ReadToEnd();
                }

            }
            Form1.ActiveForm.Text = "Mi Compilador - " + archivo;
            compilarSoluciónToolStripMenuItem.Enabled = true;
        }
        private void guardar()
        {
            SaveFileDialog VentanaGuardar = new SaveFileDialog();
            VentanaGuardar.Filter = "Texto|*.c";
            if (archivo != null)
            {
                using (StreamWriter Escribir = new StreamWriter(archivo))
                {
                    Escribir.Write(CajaTxt1.Text);
                }
            }
            else
            {
                if (VentanaGuardar.ShowDialog() == DialogResult.OK)
                {
                    archivo = VentanaGuardar.FileName;
                    using (StreamWriter Escribir = new StreamWriter(archivo))
                    {
                        Escribir.Write(CajaTxt1.Text);
                    }
                }
            }
            Form1.ActiveForm.Text = "Mi Compilador - " + archivo;
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
                using (StreamWriter Escribir = new StreamWriter(archivo))
                {
                    Escribir.Write(CajaTxt1.Text);
                }
            }
            Form1.ActiveForm.Text = "Mi Compilador - " + archivo;
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

        // ==========================================
        //           ANALIZADOR LÉXICO
        // ==========================================
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
            if (P_Reservadas.IndexOf(elemento.ToLower()) >= 0) return true;
            return false;
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
                    Escribir.Write("identificador\n");
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

        // ==========================================
        //           MANEJO DE ERRORES Y UTILIDADES
        // ==========================================

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

        private void Error(string token, string esperado)
        {
            TxtboxSalida.AppendText($"Error: se esperaba '{esperado}', pero se encontró '{token}', línea {linea_del_token}\n");
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

            return t;
        }

        // ==========================================
        //           ANALIZADORES
        // ==========================================

        private void AnalizadorLexico()
        {
            Numero_linea = 1;
            N_error = 0;

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

        private void AnalizadorSintactico()
        {
            Numero_linea = 1;
            Leer = new StreamReader(archivoback);
            token = NextToken();
            Cabecera();
            Leer.Close();
        }

        private void Cabecera()
        {
            if (token == null || token == "Fin") return;

            switch (token)
            {
                case "#":
                    token = NextToken();
                    if (token == null) { Error("Directiva incompleta después de '#'"); return; }
                    Directiva_proc();
                    token = NextToken();
                    Cabecera();
                    break;

                case "int":
                case "float":
                case "double":
                case "char":
                case "void":
                case "Tipo":
                    string tipo = token;
                    token = NextToken();
                    string id = token;

                    token = NextToken();

                    if (token == "(")
                    {
                        Parametros();
                        BloqueDeSentencias();

                        token = NextToken();
                        Cabecera();
                    }
                    else
                    {
                        Declaracion_Variable_Global_Logica(id);
                        token = NextToken();
                        Cabecera();
                    }
                    break;

                default:
                    token = NextToken();
                    Cabecera();
                    break;
            }
        }

        private void Parametros()
        {
            token = NextToken();
            if (token == ")") { token = NextToken(); return; }

            while (token != ")" && token != "Fin")
            {
                if (token != "int" && token != "float" && token != "char" && token != "double")
                    Error(token, "tipo de dato");

                token = NextToken();
                if (token != "identificador")
                {
                    Error(token, "identificador");
                    token = NextToken();
                    if (token == ")") break;
                }
                else
                {
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


        // ==========================================
        //           BLOQUES Y SENTENCIAS
        // ==========================================

        private void BloqueDeSentencias()
        {
            if (token != "{") { Error(token, "{"); return; }
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

                    case "if": EstructuraIf(); break;
                    case "while": EstructuraWhile(); break;
                    case "do": EstructuraDoWhile(); break;
                    case "for": EstructuraFor(); break;
                    case "switch": EstructuraSwitch(); break;

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
                            Expresion();
                        }

                        if (token != ";") Error(token, ";");
                        token = NextToken();
                        break;

                    case "identificador":
                    case "printf":
                        Sentencia();
                        break;

                    case ";": token = NextToken(); break;

                    case "{":
                        BloqueDeSentencias();
                        token = NextToken();
                        break;

                    case "++":
                    case "--":
                        {
                            string inc = token;
                            token = NextToken();
                            if (token != "identificador") { Error(token, "identificador"); break; }
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
        }
        private void SentenciaOBloque()
        {
            if (token == "{")
            {
                BloqueDeSentencias();
                token = NextToken();
                return;
            }

            if (token == "identificador" || token == "printf")
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
            token = NextToken();

            if (token == "(")
            {
                token = NextToken();
                if (token != ")")
                {
                    while (true)
                    {
                        Expresion();
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

            bool tieneIndexacion = false;
            while (token == "[")
            {
                tieneIndexacion = true;
                token = NextToken();
                Condicion();
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
                string asign = token;
                token = NextToken();
                Expresion();
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



        // ==========================================
        //           ESTRUCTURAS DE CONTROL
        // ==========================================

        private void EstructuraIf()
        {
            token = NextToken();
            if (token != "(") { Error(token, "("); return; }
            token = NextToken();
            Expresion();
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
            Expresion();
            if (token != ")") { Error(token, ")"); return; }
            token = NextToken();

            SentenciaOBloque();
        }


        private void EstructuraFor()
        {
            token = NextToken();
            if (token != "(") { Error(token, "("); return; }
            token = NextToken();

            if (token == "int" || token == "float" || token == "double" || token == "char")
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
                Expresion();
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
                        Expresion();
                    }
                    else
                    {
                        if (token == "++" || token == "--")
                        {
                            token = NextToken();
                        }
                        else
                        {
                            Expresion();
                        }
                    }

                }
                else
                {
                    Expresion();
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
            Expresion();
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
            Expresion();
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
                else if (token == "identificador" || token == "printf") Sentencia();
                else if (token == "if") EstructuraIf();
                else if (token == "while") EstructuraWhile();
                else if (token == "for") EstructuraFor();
                else if (token == "{")
                {
                    BloqueDeSentencias();
                    token = NextToken();
                }
                else token = NextToken();
            }
        }

        // ==========================================
        //           AUXILIARES Y DECLARACIONES
        // ==========================================
        private bool EsOperador(string t)
        {
            return t == "+" || t == "-" || t == "*" || t == "/" || t == "%" ||
                   t == "=" || t == "==" || t == "!=" || t == ">" || t == "<" ||
                   t == ">=" || t == "<=" || t == "&&" || t == "||" || t == "!";
        }

        //
        // ---------- NUEVO: ANALIZADOR DE EXPRESIONES (POR NIVELES) --------------
        //

        private void Expresion()
        {
            Condicion();
        }

        private void Condicion()
        {
            TerminoAND();

            while (token == "||" || token == "|")
            {
                if (token == "|")
                {
                    string saved = token;
                    token = NextToken();
                    if (token != "|")
                    {
                        Error(saved, "||");
                        return;
                    }
                    token = NextToken();
                }
                else
                {
                    token = NextToken();
                }
                TerminoAND();
            }
        }

        private void TerminoAND()
        {
            ExpresionIgualdad();

            while (token == "&&" || token == "&")
            {
                if (token == "&")
                {
                    string saved = token;
                    token = NextToken();
                    if (token != "&")
                    {
                        Error(saved, "&&");
                        return;
                    }
                    token = NextToken();
                }
                else
                {
                    token = NextToken();
                }
                ExpresionIgualdad();
            }
        }

        private void ExpresionIgualdad()
        {
            ExpresionRelacional();

            bool esOperador = false;

            if (token == "!" || token == "!=")
            {
                if (token == "!")
                {
                    token = NextToken();
                    if (token == "=")
                    {
                        esOperador = true;
                        token = NextToken();
                    }
                    else
                    {
                        Error("!", "operador de igualdad '!='");
                        return;
                    }
                }
                else
                {
                    esOperador = true;
                    token = NextToken();
                }
            }
            else if (token == "==" || token == "=")
            {
                if (token == "=")
                {
                    token = NextToken();
                    if (token == "=")
                    {
                        esOperador = true;
                        token = NextToken();
                    }
                    else
                    {
                        return;
                    }
                }
                else
                {
                    esOperador = true;
                    token = NextToken();
                }
            }

            if (esOperador)
            {
                ExpresionRelacional();
            }
        }

        private void ExpresionRelacional()
        {
            ExpresionAritmetica();

            string op = token;
            bool esOperador = false;

            if (op == "<" || op == ">" || op == "<=" || op == ">=")
            {
                esOperador = true;
                token = NextToken();

                if ((op == "<" || op == ">") && token == "=")
                {
                    token = NextToken();
                }
            }

            if (esOperador)
            {
                ExpresionAritmetica();
            }
        }

        private void ExpresionAritmetica()
        {
            Termino();

            while (token == "+" || token == "-")
            {
                token = NextToken();
                Termino();
            }
        }

        private void Termino()
        {
            Factor();
            while (token == "*" || token == "/" || token == "%")
            {
                token = NextToken();
                Factor();
            }
        }

        private bool EsTipo(string t)
        {
            return t == "int" || t == "float" || t == "double" || t == "char" || t == "void";
        }

        private void Factor()
        {
            if (token == "++" || token == "--" || token == "!" || token == "-" || token == "+")
            {
                token = NextToken();
                Factor();
                return;
            }

            if (token == "(")
            {
                token = NextToken();
                if (token != null && EsTipo(token))
                {
                    token = NextToken();
                    if (token != ")") { Error(token, ")"); return; }
                    token = NextToken();
                    Factor();
                    return;
                }
                else
                {
                    Condicion();
                    if (token != ")") { Error(token, ")"); return; }
                    token = NextToken();
                    return;
                }
            }

            if (token == "identificador")
            {
                token = NextToken();

                if (token == "(")
                {
                    token = NextToken();
                    if (token != ")")
                    {
                        while (true)
                        {
                            Condicion();
                            if (token == ",") { token = NextToken(); continue; }
                            else if (token == ")") break;
                            else { Error(token, ", o )"); return; }
                        }
                    }
                    token = NextToken();
                    while (token == "++" || token == "--")
                    {
                        token = NextToken();
                    }
                    return;
                }

                while (token == "[")
                {
                    token = NextToken();
                    Condicion();
                    if (token != "]") { Error(token, "]"); return; }
                    token = NextToken();
                }

                while (token == "++" || token == "--")
                {
                    token = NextToken();
                }

                return;
            }

            if (token == "numero_entero" || token == "numero_real" || token == "Cadena" || token == "caracter")
            {
                token = NextToken();

                while (token == "++" || token == "--")
                {
                    token = NextToken();
                }
                return;
            }

            Error(token, "identificador, número o '('");
            return;
        }

        private void Declaracion_Local()
        {
            token = NextToken();
            string id = token;
            token = NextToken();
            Declaracion_Variable_Global_Logica(id);
            token = NextToken();
        }

        private void Declaracion_Variable_Global_Logica(string identificador_actual)
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
                        Expresion();
                    }
                }
            }

            ProcesarDeclarador();

            while (token == ",")
            {
                token = NextToken();
                if (token != "identificador") { Error(token, "identificador"); return; }
                token = NextToken();
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
                else { Error(token, "valor o sub-arreglo"); return; }

                if (token == ",") token = NextToken();
                else if (token == "}") break;
                else { Error(token, "',' o '}'"); return; }
            }
            token = NextToken();
        }

        private int Directiva_proc()
        {
            while (token == "LF") token = Leer.ReadLine();
            if (token == null) { Error("Directiva incompleta"); return 0; }

            switch (token)
            {
                case "include":
                    token = Leer.ReadLine();
                    while (token == "LF") token = Leer.ReadLine();
                    if (token == null) { Error("Include incompleto"); return 0; }
                    return Directiva_include();

                case "define":
                    token = Leer.ReadLine();
                    while (token == "LF") token = Leer.ReadLine();
                    if (token == null) { Error("define incompleto"); return 0; }
                    return 1;

                default:
                    Error("include o define");
                    return 0;
            }
        }

        private int Directiva_include()
        {
            while (token == "LF") { Numero_linea++; token = Leer.ReadLine(); }
            if (token == null) return 0;

            if (token == "<")
            {
                token = Leer.ReadLine();
                if (token == null) { Error("libreria inválida"); return 0; }
                token = Leer.ReadLine();
                if (token != ">") { Error(token, ">"); return 0; }
                return 1;
            }
            else if (token == "Cadena") return 1;

            Error("Formato include");
            return 0;
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
    }
}
