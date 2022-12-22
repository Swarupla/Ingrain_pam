import spacy
import neuralcoref
nlp = spacy.load('en')
neuralcoref.add_to_pipe(nlp)

class CoreferenceResolution:
	
    def __init__(self):
        pass

    def reference(self, inp):
        return_value = {}
        return_value["is_success"] = False
        return_value["message"] = ""
        return_value["response_data"] = []
        text = inp
        doc = nlp(inp)
        def remove_dup(lst):
            a_lst = []
            for i in lst:
                if i not in a_lst:
                    a_lst.append(i)
            return a_lst
        for i in doc._.coref_clusters:
            bb = str(i).split(":")
            bb0 = bb[0].strip()
            bb1 = (bb[1][2:-1].split(","))
            bb1 = remove_dup([l.strip() for l in bb1])
            for j in bb1:
                if j != bb1:
                    inp = inp.replace(j,j+" ["+bb0+"]")
        data = [{"response": inp, "text": text}]
        return_value["is_success"] = True
        return_value["message"] = ""
        return_value["response_data"] = data

        #return inp
        return return_value

		
if __name__ == "__main__": 
    returnValue = {}
    returnValue["is_success"] = ""
    returnValue["message"] = ""
    returnValue["response"] = ""
    sentence = "Teja is a good boy. He has a cat."
    coref = CoreferenceResolution()
    returnValue["response"] = coref.reference(sentence)
    print(returnValue)