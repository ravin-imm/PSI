// ⓅⓈⒾ  ●  Pascal Language System  ●  Academy'23
// TypeAnalyze.cs ~ Type checking, type coercion
// ─────────────────────────────────────────────────────────────────────────────
namespace PSI;
using static NType;
using static Token.E;

public class TypeAnalyze : Visitor<NType> {
   public TypeAnalyze () {
      mSymbols = SymTable.Root;
   }
   SymTable mSymbols;

   #region Declarations ------------------------------------
   public override NType Visit (NProgram p) 
      => Visit (p.Block);
   
   public override NType Visit (NBlock b) {
      mSymbols = new SymTable { Parent = mSymbols };
      Visit (b.Declarations); Visit (b.Body);
      mSymbols = mSymbols.Parent;
      return Void;
   }

   public override NType Visit (NDeclarations d) {
      Visit (d.Consts); Visit (d.Vars); return Visit (d.Funcs);
   }
   public override NType Visit (NConstDecl c) {
      if (mSymbols.Consts.Any (a => a.Name.Text == c.Name.Text))
         throw new ParseException (c.Name, "Constant with the same name already declared");
      c.Value.Accept (this);
      mSymbols.Consts.Add (c);
      return c.Value.Type;
   }
   public override NType Visit (NVarDecl d) {
      if (mSymbols.Consts.Any (a => a.Name.Text == d.Name.Text))
         throw new ParseException (d.Name, "Constant with the same name already declared");
      if (mSymbols.Vars.Any (a => a.Name.Text == d.Name.Text))
         throw new ParseException (d.Name, "Variable with the same name already declared");
      mSymbols.Vars.Add (d);
      return d.Type;
   }
   public override NType Visit (NFnDecl f) {
      if (mSymbols.Consts.Any (a => a.Name.Text == f.Name.Text))
         throw new ParseException (f.Name, "Constant with the same function name already declared");
      if (mSymbols.Vars.Any (a => a.Name.Text == f.Name.Text))
         throw new ParseException (f.Name, "Variable with the same function name already declared");
      if (mSymbols.Funcs.Any (a => a.Name.Text == f.Name.Text))
         throw new ParseException (f.Name, "Function with the same function name already declared");
      mSymbols.Funcs.Add (f);
      foreach (var param in f.Params) param.Accept (this);

      // The variables or the constants inside a function cannot have the function name or the 
      // function parameters name
      if (f.Body != null) {
         var decls = f.Body.Declarations;
         var v = decls.Vars.FirstOrDefault (a => { var t = a.Name.Text; return (t == f.Name.Text || f.Params.Any (b => b.Name.Text == t));});
         if (v != null) throw new ParseException (v.Name, "Variable cannot have either the function name or the function parameter name");
         var c = decls.Consts.FirstOrDefault (a => { var t = a.Name.Text; return (t == f.Name.Text || f.Params.Any (b => b.Name.Text == t));});
         if (c != null) throw new ParseException (c.Name, "Constant cannot have either the function name or the function parameter name");
         f.Body.Accept (this);
      }
      return f.Return;
   }
   #endregion

   #region Statements --------------------------------------
   public override NType Visit (NCompoundStmt b)
      => Visit (b.Stmts);

   public override NType Visit (NAssignStmt a) {
      var nType = mSymbols.Find (a.Name.Text) switch {
         NVarDecl v => v.Type,
         NFnDecl f => f.Return, // A function assignment statement like Fibo := prod
         _ => throw new ParseException (a.Name, "Unknown variable")
      };
      a.Expr.Accept (this);
      a.Expr = AddTypeCast (a.Name, a.Expr, nType);
      return nType;
   }
   
   NExpr AddTypeCast (Token token, NExpr source, NType target) {
      if (source.Type == target) return source;
      bool valid = (source.Type, target) switch {
         (Int, Real) or (Char, Int) or (Char, String) => true,
         _ => false
      };
      if (!valid) throw new ParseException (token, "Invalid type");
      return new NTypeCast (source) { Type = target };
   }

   public override NType Visit (NWriteStmt w)
      => Visit (w.Exprs);

   public override NType Visit (NIfStmt f) {
      f.Condition.Accept (this);
      f.IfPart.Accept (this); f.ElsePart?.Accept (this);
      return Void;
   }

   public override NType Visit (NForStmt f) {
      f.Start.Accept (this); f.End.Accept (this); f.Body.Accept (this);
      return Void;
   }

   public override NType Visit (NReadStmt r) {
      var v = r.Vars.FirstOrDefault (a => mSymbols.Find (a.Text) is not NVarDecl);
      if (v != null) throw new ParseException (v, "Unknown variable");
      return Void;
   }

   public override NType Visit (NWhileStmt w) {
      w.Condition.Accept (this); w.Body.Accept (this);
      return Void; 
   }

   public override NType Visit (NRepeatStmt r) {
      Visit (r.Stmts); r.Condition.Accept (this);
      return Void;
   }

   public override NType Visit (NCallStmt c) {
      if (mSymbols.Find (c.Name.Text) is NFnDecl d && d.Return is Void) {
         var pLen = c.Params.Length;
         if (pLen != d.Params.Length)
            throw new ParseException (c.Name, $"No overload for procedure {c.Name.Text} takes {pLen} arguments");
         for (int i = 0; i < pLen; i++) {
            c.Params[i].Accept (this);
            c.Params[i] = AddTypeCast (c.Name, c.Params[i], d.Params[i].Type);
         }
         return Void;
      }
      throw new ParseException (c.Name, "Unknown function");
   }
   #endregion

   #region Expression --------------------------------------
   public override NType Visit (NLiteral t) {
      t.Type = t.Value.Kind switch {
         L_INTEGER => Int, L_REAL => Real, L_BOOLEAN => Bool, L_STRING => String,
         L_CHAR => Char, _ => Error,
      };
      return t.Type;
   }

   public override NType Visit (NUnary u) 
      => u.Expr.Accept (this);

   public override NType Visit (NBinary bin) {
      NType a = bin.Left.Accept (this), b = bin.Right.Accept (this);
      bin.Type = (bin.Op.Kind, a, b) switch {
         (ADD or SUB or MUL or DIV, Int or Real, Int or Real) when a == b => a,
         (ADD or SUB or MUL or DIV, Int or Real, Int or Real) => Real,
         (MOD, Int, Int) => Int,
         (ADD, String, _) => String, 
         (ADD, _, String) => String,
         (LT or LEQ or GT or GEQ, Int or Real, Int or Real) => Bool,
         (LT or LEQ or GT or GEQ, Int or Real or String or Char, Int or Real or String or Char) when a == b => Bool,
         (EQ or NEQ, _, _) when a == b => Bool,
         (EQ or NEQ, Int or Real, Int or Real) => Bool,
         (AND or OR, Int or Bool, Int or Bool) when a == b => a,
         _ => Error,
      };
      if (bin.Type == Error)
         throw new ParseException (bin.Op, "Invalid operands");
      var (acast, bcast) = (bin.Op.Kind, a, b) switch {
         (_, Int, Real) => (Real, Void),
         (_, Real, Int) => (Void, Real), 
         (_, String, not String) => (Void, String),
         (_, not String, String) => (String, Void),
         _ => (Void, Void)
      };
      if (acast != Void) bin.Left = new NTypeCast (bin.Left) { Type = acast };
      if (bcast != Void) bin.Right = new NTypeCast (bin.Right) { Type = bcast };
      return bin.Type;
   }

   public override NType Visit (NIdentifier d) =>
      mSymbols.Find (d.Name.Text) switch {
         NVarDecl v => d.Type = v.Type,
         NConstDecl c => d.Type = c.Value.Accept (this),
         NFnDecl f => d.Type = f.Return, // A function can be recursive
         _ => throw new ParseException (d.Name, "Unknown variable")
      };

   public override NType Visit (NFnCall f) {
      if (mSymbols.Find (f.Name.Text) is NFnDecl d) {
         var pLen = f.Params.Length;
         if (pLen != d.Params.Length)
            throw new ParseException (f.Name, $"No overload for function {f.Name.Text} takes {pLen} arguments");
         for (int i = 0; i < pLen; i++) {
            f.Params[i].Accept (this);
            f.Params[i] = AddTypeCast (f.Name, f.Params[i], d.Params[i].Type);
         }
         return f.Type = d.Return;
      }
      throw new ParseException (f.Name, "Unknown function");
   }

   public override NType Visit (NTypeCast c) {
      c.Expr.Accept (this); return c.Type;
   }
   #endregion

   NType Visit (IEnumerable<Node> nodes) {
      foreach (var node in nodes) node.Accept (this);
      return NType.Void;
   }
}
