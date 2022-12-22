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
import stat


if platform.system() == 'Linux':
    work_dir = '/IngrAIn_Python'
elif platform.system() == 'Windows':
    work_dir = '\IngrAIn_Python'
	

Ingrain_path = os.getcwd() + work_dir
local_path=os.getcwd()
print(Ingrain_path)
print(local_path)

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
exclude_directories=set(['Cythonize','main','SSAIutils','visualizations'])

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
not_needed_files=['MigrateEnDe.py','uniqueIdentifierchanges.py','requirements.txt','Textclassification.py','Textclassification_service.py','data_quality_check.py','__init__.py','realone.py','temp.py','k_means_RandomizedSearch.py','Affinity_Propagation_Clustering_Randomized_Search.py','visualizations.py','uniqueIdentifierchanges.py']


not_needed_path=[]
for i in not_needed_files:
    for j,k in enumerate(allpyfiles_path):
        if k.endswith(i)==True:
            not_needed_path.append(k)

for i in not_needed_path:
    try:
        allpyfiles_path.remove(i)
    except ValueError:
        pass
            
for i in not_needed_files:
    try:
        allpyfiles.remove(i)
    except ValueError:
        pass
            

destination=cfilespath

if platform.system()=='Windows':
    destination=cfilespath
elif platform.system()=='Linux':
    destination=cfilespath


for i in allpyfiles_path:
    shutil.copy(i,destination)

print("allfiles",allpyfiles)
print("destination",destination)
os.chdir(destination)
print("sys",sys.argv[0],sys.argv[1])
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
        print(x)
        if x.endswith('.py') or x.endswith('.ini') or x.endswith('.pyd'):
            l.append(pyp+'\\'+x) 
        elif not (x.endswith('.py') or x.endswith('.ini') or x.endswith('.pyd')):
            ignore.append(pyp+'\\'+x)
        elif x!='__pycache__' and x!='User Help' and x!='PyLogs':
            print(x)
            explore_path(pyp+'\\'+x)
    
if platform.system()=='Windows':
    final_pyd_path=destination+'\\IngrAIn_Python'
elif platform.system()=='Linux':
    final_pyd_path=destination+'/IngrAIn_Python'
print("final_pyd_path",final_pyd_path)
#base='D:\\Apps\\Ingrain-Core-R2.1-WindowsService\\IngrAIn_Python'
#hjk='C:\\Users\\saurav.b.mondal\\Desktop\\Cythonize\\IngrAIn_Python'

if os.path.exists(final_pyd_path):
    shutil.rmtree(final_pyd_path,ignore_errors=True)
    os.mkdir(final_pyd_path)
else:
    os.mkdir(final_pyd_path)

def copytree(src, dst, symlinks=False, ignore=None):
    if not os.path.exists(dst):
        os.makedirs(dst)
    for item in os.listdir(src):
        s = os.path.join(src, item)
        d = os.path.join(dst, item)
        if os.path.isdir(s):
            copytree(s, d, symlinks, ignore)
        else:
            if not os.path.exists(d) or os.stat(s).st_mtime - os.stat(d).st_mtime > 1:
                shutil.copy2(s, d)

copytree(pyfilespath,final_pyd_path)

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
print("allpyfiles1",allpyfiles1)


#allpyfiles1[:]=[x for x in allpyfiles1 if x!='__init__.py']


#l=[]
#ignore=[]
#explore_path(hjk+"\\lib.win-amd64-3.7")

##############################################################################################
if platform.system()=='Windows':
    extensions=('.pyd')
    pyfilespath_lib=destination+"\\build\\lib.win-amd64-3.9"
elif platform.system()=='Linux':
    extensions=('.so')
    pyfilespath_lib=destination+"/build/lib.linux-x86_64-3.9"
exclude_directories=set(['Cythonize','main','SSAIutils'])
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
print("allpyfiles_path_pyd",allpyfiles_path_pyd)
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
def explore_path1(final_pyd_path):
  
    for x in os.listdir(final_pyd_path):
        print(x)
        if x.endswith('.py') or x.endswith('.ini') or x.endswith('.pyd'):
            l.append(final_pyd_path+'\\'+x) 
        elif not (x.endswith('.py') or x.endswith('.ini') or x.endswith('.pyd')):
            ignore.append(final_pyd_path+'\\'+x)
        elif x=='__pycache__' and x=='User Help' and x=='PyLogs':
            ignore.append(final_pyd_path+'\\'+x)
            explore_path1(final_pyd_path+'\\'+x)
			
if platform.system()=='Windows':
    
    root_src_dir=local_path+"\\Cythonize\\IngrAIn_Python\\"
    root_dst_dir=local_path+"\\IngrAIn_Python\\"
elif platform.system()=='Linux':
    root_src_dir=local_path+"//Cythonize//IngrAIn_Python//"
    root_dst_dir=local_path+"//IngrAIn_Python//"

def copytree_final(src, dst, symlinks=False, ignore=None):
    if not os.path.exists(dst):
        os.makedirs(dst)
    for item in os.listdir(src):
        s = os.path.join(src, item)
        d = os.path.join(dst, item)
        if os.path.isdir(s):
            copytree(s, d, symlinks, ignore)
        else:
            if not os.path.exists(d) or os.stat(s).st_mtime - os.stat(d).st_mtime > 1:
                shutil.copy2(s, d)
shutil.rmtree(root_dst_dir)
copytree_final(root_src_dir,root_dst_dir)
try:           
    if platform.system()=='Windows': 
        os.chmod(local_path+"\\Cythonize\\", stat.S_IWRITE)
        shutil.rmtree(local_path+"\\Cythonize\\")
    elif platform.system()=='Linux':
        shutil.rmtree(local_path+"//Cythonize//")
except:
    pass
    
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
   