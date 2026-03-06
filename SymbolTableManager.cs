using System;
using System.Collections.Generic;
using System.Linq;

namespace Editordetexto
{
    public class SimboloVariable
    {
        public string Nombre { get; }
        public string Tipo { get; }
        public int Direccion { get; }

        public SimboloVariable(string nombre, string tipo, int direccion)
        {
            Nombre = nombre;
            Tipo = tipo;
            Direccion = direccion;
        }
    }

    public class SimboloFuncion
    {
        public string Nombre { get; }
        public string TipoRetorno { get; }
        public List<string> TiposParametros { get; }

        public int NumeroParametros => TiposParametros.Count;

        public SimboloFuncion(string nombre, string tipoRetorno)
        {
            Nombre = nombre;
            TipoRetorno = tipoRetorno;
            TiposParametros = new List<string>();
        }
    }

    public class SymbolTableManager
    {
        private readonly Dictionary<string, SimboloFuncion> funciones =
            new Dictionary<string, SimboloFuncion>(StringComparer.Ordinal);

        // pila de ámbitos: cada ámbito es diccionario nombre -> variable
        private readonly Stack<Dictionary<string, SimboloVariable>> pilaAmbitos =
            new Stack<Dictionary<string, SimboloVariable>>();

        public int DireccionActual { get; private set; } = 0;

        public SymbolTableManager()
        {
            Reset();
        }

        public void Reset()
        {
            funciones.Clear();
            pilaAmbitos.Clear();
            DireccionActual = 0;
            // crear ámbito global
            EnterScope();
        }

        public void AddBuiltinFunction(string nombre, string tipoRetorno)
        {
            if (!funciones.ContainsKey(nombre))
                funciones[nombre] = new SimboloFuncion(nombre, tipoRetorno);
        }

        public void EnterScope()
        {
            pilaAmbitos.Push(new Dictionary<string, SimboloVariable>(StringComparer.Ordinal));
        }

        public void ExitScope()
        {
            if (pilaAmbitos.Count > 1) // no eliminar ámbito global
                pilaAmbitos.Pop();
        }

        public bool AddVariable(string nombre, string tipo, out string error)
        {
            error = null;
            if (pilaAmbitos.Count == 0)
            {
                error = "No hay ámbitos disponibles.";
                return false;
            }

            var current = pilaAmbitos.Peek();
            if (current.ContainsKey(nombre))
            {
                error = $"La variable '{nombre}' ya fue declarada en este ámbito.";
                return false;
            }

            current[nombre] = new SimboloVariable(nombre, tipo, DireccionActual);
            DireccionActual += 4;
            return true;
        }

        public bool AddFunction(string nombre, string tipoRetorno, out string error)
        {
            error = null;
            if (funciones.ContainsKey(nombre))
            {
                error = $"La función '{nombre}' ya fue declarada.";
                return false;
            }
            funciones[nombre] = new SimboloFuncion(nombre, tipoRetorno);
            return true;
        }

        public bool VariableExistsInCurrentScope(string nombre)
        {
            if (pilaAmbitos.Count == 0) return false;
            return pilaAmbitos.Peek().ContainsKey(nombre);
        }

        public bool VariableDeclared(string nombre)
        {
            foreach (var amb in pilaAmbitos)
            {
                if (amb.ContainsKey(nombre)) return true;
            }
            return false;
        }

        public bool FunctionExists(string nombre)
        {
            return funciones.ContainsKey(nombre);
        }

        public SimboloVariable GetVariable(string nombre)
        {
            foreach (var amb in pilaAmbitos)
            {
                if (amb.TryGetValue(nombre, out var v)) return v;
            }
            return null;
        }

        public SimboloFuncion GetFunction(string nombre)
        {
            funciones.TryGetValue(nombre, out var f);
            return f;
        }

        public IEnumerable<SimboloFuncion> AllFunctions() => funciones.Values;

        // <-- CORRECCIÓN: convertir cada diccionario a su colección de valores
        public IEnumerable<IEnumerable<SimboloVariable>> AllScopes()
        {
            // devolvemos los valores (SimboloVariable) de cada ámbito en el orden de la pila
            // si prefieres el orden del más externo al más interno, puedes invertir la pila aquí.
            return pilaAmbitos.Select(amb => (IEnumerable<SimboloVariable>)amb.Values.AsEnumerable());
        }
    }
}