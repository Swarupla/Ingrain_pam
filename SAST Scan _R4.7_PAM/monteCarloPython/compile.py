from distutils.core import setup
from distutils.extension import Extension
from Cython.Distutils import build_ext
import os
from os import listdir
from os.path import isfile, join
import platform
import shutil
import time
#/var/www/myWizard.IngrAIn.SelfServiceAI/ingrain_venv/bin/python compile.py build 
import json
import sys


#if platform.system() == 'Linux':
#    work_dir = '/IngrAIn_Python'
#elif platform.system() == 'Windows':
 #   work_dir = '\IngrAIn_Python'
	

Ingrain_path = os.getcwd()
local_path=os.getcwd()


sys.argv.insert(1,"build")
#pyfilespath="C:\\Users\\harsh.nandedkar\\Desktop\\IngrAIn_Python"
if platform.system()=='Windows':
    pyfilespath=Ingrain_path
elif platform.system()=='Linux':
    pyfilespath=Ingrain_path

#'D:\\Apps\\Ingrain-Core-R2.1-WindowsService\\IngrAIn_Python'   
#/usr/local/bin/anaconda3/bin/python3.7 compile.py build    
allc_files=[]

if platform.system()=='Windows':
    
    cfilespath=local_path+"\\Cythonize"
    if not os.path.exists(cfilespath):
        os.makedirs(cfilespath)
elif platform.system()=='Linux':
    cfilespath=local_path+"/Cythonize"
    if not os.path.exists(cfilespath):
        os.makedirs(cfilespath,mode=0o777,exist_ok=False)

for file in os.listdir(cfilespath):
    if file.endswith(".c"):
        allc_files.append(file)

if len(allc_files)!=0:
    for file in allc_files:
        if platform.system()=='Windows':
            os.remove(cfilespath+'\\'+file)
        elif platform.system()=='Linux':
            os.remove(cfilespath+'/'+file)
        

extensions=('.py')
exclude_directories=set(['Cythonize','__pycache__','mc_venv','offlinePackages','offlinePackages_Linux'])

allpyfiles=[]
allpyfiles_path=[]

for dirname,dirs,files in os.walk(pyfilespath):
    dirs[:]=[d for d in dirs if d not in exclude_directories]
    for fname in files:
        if (fname.endswith(extensions)):
            allpyfiles.append(fname)
            fpath= os.path.join(dirname,fname)
            allpyfiles_path.append(fpath)
            #print(fpath)
allpyfiles[:]=[x for x in allpyfiles if x!='__init__.py']
not_needed_files=['Auth.py','file_encryptor.py','GetPhoenixData.py','IngestPhoenixData.py','invokeMonteCarlo.py','Mongo.py','Sensitivity.py','testIfEncrypted.py','utils.py','wsgi.py','compile.py']

not_needed_path=[]
for i in not_needed_files:
    for j,k in enumerate(allpyfiles_path):
        if k.endswith(i)==True:
            not_needed_path.append(k)

src_file = list(set(allpyfiles_path) - set(not_needed_path))

try:
    for i in not_needed_path:
        allpyfiles_path.remove(i)
except ValueError:
    error_encounterd = "ValueError"
  
            
try:
    for i in not_needed_files:
        allpyfiles.remove(i)
except ValueError:
    print("exception")
    pass
            

destination=cfilespath

if platform.system()=='Windows':
    destination=cfilespath
elif platform.system()=='Linux':
    destination=cfilespath


for i in allpyfiles_path:
    shutil.copy(i,destination)


os.chdir(destination)
ext_modules = []
for i in allpyfiles:
   ext_modules.append(Extension(i.strip('.py'), [i]))

setup(
    name = 'SSAI',
    cmdclass = {'build_ext': build_ext},
    ext_modules = ext_modules
)


l=[]
ignore=[]
def explore_path(pyp):
  
    for x in os.listdir(pyp):
        if x.endswith('.py') or x.endswith('.ini') or x.endswith('.pyd'):
            l.append(pyp+'\\'+x) 
        elif not (x.endswith('.py') or x.endswith('.ini') or x.endswith('.pyd')):
            ignore.append(pyp+'\\'+x)
        elif x!='__pycache__' and x!='User Help' and x!='PyLogs':
            explore_path(pyp+'\\'+x)
    
if platform.system()=='Windows':
    final_pyd_path=destination
elif platform.system()=='Linux':
    final_pyd_path=destination

extensions=('.py')
#exclude_directories=set(['Cythonize','main','SSAIutils'])
pyfilespath_pyd=final_pyd_path
allpyfiles1=[]
allpyfiles_path1=[]
for dirname,dirs,files in os.walk(pyfilespath_pyd):
    #dirs[:]=[d for d in dirs if d not in exclude_directories]
    for fname in files:
        if (fname.endswith(extensions)):
            allpyfiles1.append(fname)
            fpath= os.path.join(dirname,fname)
            allpyfiles_path1.append(fpath)

dst_path = Ingrain_path
dst_files = []
for dirname,dirs,files in os.walk(dst_path):
    #dirs[:]=[d for d in dirs if d not in exclude_directories]
    for fname in files:
        if (fname.endswith(extensions)):
            dst_files.append(fname)


##############################################################################################
if platform.system()=='Windows':
    extensions=('.pyd')
    pyfilespath_lib=destination+"\\build\\lib.win-amd64-3.9"
elif platform.system()=='Linux':
    extensions=('.so')
    pyfilespath_lib=destination+"/build/lib.linux-x86_64-3.9"
exclude_directories=set(['Cythonize','__pycache__','mc_venv','offlinePackages','offlinePackages_Linux'])
#xyz="C:\\Users\\saurav.b.mondal\\Desktop\\Cythonize\\build"

allpyfiles_pyd=[]
allpyfiles_path_pyd=[]
for dirname,dirs,files in os.walk(pyfilespath_lib):
    dirs[:]=[d for d in dirs if d not in exclude_directories]
    for fname in files:
        if (fname.endswith(extensions)):
            allpyfiles_pyd.append(fname)
            fpath= os.path.join(dirname,fname)
            allpyfiles_path_pyd.append(fpath)
#print(allpyfiles_path_pyd)


for i in allpyfiles_path1:
  for x in allpyfiles_path_pyd:
        if str(os.path.basename(i)).split('.')[0]==str(os.path.basename(x)).split('.')[0]:
            shutil.copy(x,i.split(os.path.basename(i))[0])
            #time.sleep(1)
            if os.path.exists(i):
                os.remove(i)

l=[]
ignore=[]

for i in src_file:
    if os.path.exists(i):
        os.remove(i)
    


if platform.system()=='Windows':
    
    root_src_dir=local_path+"\\Cythonize\\"
    root_dst_dir=local_path
elif platform.system()=='Linux':
    root_src_dir=local_path+"//Cythonize//"
    root_dst_dir=local_path
    
src_files = os.listdir(pyfilespath_lib)
for file_name in src_files:
    full_file_name = os.path.join(pyfilespath_lib, file_name)
    if os.path.isfile(full_file_name):
        shutil.copy(full_file_name,root_dst_dir)

try:           
    if platform.system()=='Windows': 
        os.chmod(local_path+"\\Cythonize\\", stat.S_IWRITE)
        shutil.rmtree(local_path+"\\Cythonize\\")
    elif platform.system()=='Linux':
        shutil.rmtree(local_path+"//Cythonize//")
except Exception as e:
    error_encounterd = str(e.args[0])

'''
ext_modules = [
##   Extension("Data_Modification",  ["Data_Modification.py"]),
##    Extension("Data_PreProcessing",  ["Data_PreProcessing.py"]),
##    Extension("data_quality_check",  ["data_quality_check.py"]),
##    Extension("DataEncoding",  ["DataEncoding.py"])
##    Extension("Eli5_M",  ["Eli5_M.py"]),
##    Extension("Feature_Importance",  ["Feature_Importance.py"]),
##    Extension("gb_Regressor",  ["gb_Regressor.py"]),
##    Extension("Lasso_Regressor",  ["Lasso_Regressor.py"]),
##    Extension("LogisticReg",  ["LogisticReg.py"]),
##    Extension("M_Evaluation",  ["M_Evaluation.py"]),
##    Extension("Models_EstimatedRunTime",  ["Models_EstimatedRunTime.py"]),
##    Extension("publishModel",  ["publishModel.py"]),
##    Extension("Random_Forest_Regressor",  ["Random_Forest_Regressor.py"]),
##    Extension("RandomForestClassifier",  ["RandomForestClassifier.py"]),
##    Extension("regression_evaluation",  ["regression_evaluation.py"]),
##    Extension("Ridge_Regressor",  ["Ridge_Regressor.py"]),
##    Extension("smote",  ["smote.py"]),
##    Extension("SVM",  ["SVM.py"]),
   Extension("SVR_Regressor",  ["SVR_Regressor.py"]),
##    Extension("Train_Cross_Validate",  ["Train_Cross_Validate.py"]),
##    Extension("utils",  ["utils.py"]),
##    Extension("WF_Analysis",  ["WF_Analysis.py"]),
##    Extension("WF_UploadTestData",  ["WF_UploadTestData.py"]),
##    Extension("XGB",  ["XGB.py"]),
##    Extension("MissingValuesImputer",  ["MissingValuesImputer.py"]),
##      Extension("utils",  ["utils.py"])
#   ... all your modules that need be compiled ...
]
'''
   