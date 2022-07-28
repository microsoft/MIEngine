#include <unistd.h>
#include <stdio.h>
#include <limits.h>
#include <stdlib.h>

int main() {
   char *currentDir = (char*)calloc(1, PATH_MAX);
   if (getcwd(currentDir, PATH_MAX) != NULL) {
       printf("Current working dir: %s\n", currentDir);
   } else {
       perror("getcwd() error");
       return 1;
   }
   return 0;
}