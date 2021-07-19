class myException
{
public:

	int RaisedUnhandledException(int a);
	int RaisedHandledException(int b);
	int EvalFunc(int a, int b);
	int RecursiveFunc(int a);
	void RaisedThrowNewException();
	void RaisedReThrowException();
};

class newException
{
public:
	int code;
	newException(int c);

	~newException();
};