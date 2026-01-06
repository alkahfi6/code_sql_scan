using System;

namespace Samples.ExecProcConst
{
    public class ExecProcConst
    {
        public void Run(bool flag, string key)
        {
            var db = new Gateway();
            var direct = "dbo.ConstDirect";
            db.ExecProc(direct);

            var ternary = flag ? "dbo.ConstTernA" : "dbo.ConstTernB";
            db.ExecProc(ternary);

            string selected;
            switch (key)
            {
                case "x":
                    selected = "dbo.ConstSwitchX";
                    break;
                case "y":
                    selected = "dbo.ConstSwitchY";
                    break;
                default:
                    selected = "dbo.ConstSwitchDefault";
                    break;
            }
            db.ExecProc(selected);

            var interp = "dbo.ConstInterp";
            db.ExecProc($"exec {interp}");

            var format = "dbo.ConstFormat";
            db.ExecProc(string.Format("exec {0}", format));
        }
    }

    public class Gateway
    {
        public void ExecProc(string sql) { }
    }
}
