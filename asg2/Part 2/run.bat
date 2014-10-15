csc /out:Network\Network.exe Network\Network.cs 
csc /out:Middleware1\Middleware.exe Middleware1\Middleware.cs
csc /out:Middleware2\Middleware.exe Middleware2\Middleware.cs
csc /out:Middleware3\Middleware.exe Middleware3\Middleware.cs
csc /out:Middleware4\Middleware.exe Middleware4\Middleware.cs
csc /out:Middleware5\Middleware.exe Middleware5\Middleware.cs
start Network\Network.exe
start Middleware1\Middleware.exe
start Middleware2\Middleware.exe
start Middleware3\Middleware.exe
start Middleware4\Middleware.exe
start Middleware5\Middleware.exe