program Expr;
var
  i, j, fib: integer;
  exit: char;

function Fibo (n: integer) : integer;
var 
  i, prod: integer;
begin 
  prod := 1;
  for i := 1 to n do begin
    prod := prod * i;
  end;
  Fibo := prod;
end;

begin
  while true do begin
    Write ("Enter a number to calculate its Fibonacci:");
    ReadLn (j);
    fib := Fibo (j);
    WriteLn ("Fibo(", j, ") = ", fib);
    Write ("Type (y) to continue, else (n) to exit:");
    ReadLn (exit);
    if exit='n' then break;
  end;
end.