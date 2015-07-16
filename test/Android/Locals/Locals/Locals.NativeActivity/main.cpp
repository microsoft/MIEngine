/*
* Copyright (C) 2010 The Android Open Source Project
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
*      http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*
*/

#define LOGI(...) ((void)__android_log_print(ANDROID_LOG_INFO, "AndroidProject1.NativeActivity", __VA_ARGS__))
#define LOGW(...) ((void)__android_log_print(ANDROID_LOG_WARN, "AndroidProject1.NativeActivity", __VA_ARGS__))

class SimpleClass 
{
private: 
	int _a;
	int _b;

public:
	SimpleClass(int a, int b) : _a(a), _b(b) { }

	void TestMe()
	{
		_a += 0; // bp here
	}
};

class BaseClass
{
protected:
	int _a;

public:
	BaseClass(int a) : _a(a) 
	{

	}

	void TestMe()
	{
		_a += 0; // bp here;
	}
};

class DerivedClass : public BaseClass
{
protected:
	int _b;

public:
	DerivedClass(int a, int b) : BaseClass(a), _b(b)
	{

	}

	void TestMe()
	{
		_b += 0; // bp here
	}
};

void x_2()
{
	int x = 0xBEEF;

	x += 0; // bp here
}

void x_1()
{
	int x = 0xDEAD;

	x += 0; // bp here 

	x_2();

	x += 0; // bp here
}

/**
* This is the main entry point of a native application that is using
* android_native_app_glue.  It runs in its own thread, with its own
* event loop for receiving input events and doing other things.
*/
void android_main(struct android_app* state) {
	int i = 0;
	int j = 1;
	int k = 0xDEADBEEF;

	int* p = &k;

	float f = 0.2;

	char* name = "TEST NAME";
	const char* const_name = "TEST NAME";
	char name_array[10] = "TEST NAME";

	int numbers[4] = { 10, 20, 30, 40 };
	int* numbers_points[4] = { &i, &j, &k, p };

	SimpleClass simpleClass(0xDEAD, 0xBEEF);
	SimpleClass* pSimpleClass = new SimpleClass(0xDEAD, 0xBEEF);
	DerivedClass derivedClass(0xDEAD, 0xBEEF);
	DerivedClass* pDerivedClass = new DerivedClass(0xDEAD, 0xBEEF);

	char* escaped = "Hello\n\tWorld!\n";
	const char* const_escaped = "Hello\n\tWorld!\n";

	i += 0; // breakpoint here

	simpleClass.TestMe();
	pSimpleClass->TestMe();
	derivedClass.TestMe();
	pDerivedClass->TestMe();

	delete pSimpleClass;
	delete pDerivedClass;

	x_1();
}
