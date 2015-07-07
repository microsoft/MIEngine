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


class MyClass
{
private:
	int _a;
	int _b;
public:
	MyClass(int a, int b) : _a(a), _b(b) {}

	void MyClassFunc()
	{
		_a += 0;
	}

	int GetSum()
	{
		return _a + _b;
	}
	
};

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

	MyClass myClassVar(7, 8);

	i += 0; // breakpoint here, line 60

}
