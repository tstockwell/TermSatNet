# TermMark

TermMark is a SAT solver.
TermMark implements a variation of the Stalmarck method with a significant enhancement of the 0-Saturation procedure.

TermMark enhances the set of rules used in the -Saturation procedure with a set of rules that perform 
basic formula semantic rewriting of formulas to simpler forms.

Found a new reduction rule: *-.2.1 ==> *-.1.2
Found a new reduction rule: *.2*.1.3 ==> *.1*.2.3
Found a new reduction rule: *-.2-*.1.3 ==> **.1.3.2
Found a new reduction rule: **.1.2-*.1.3 ==> -*.1-*.2-.3  
Found a new reduction rule: **.1.2-*.3.2 ==> -**-.1.3.2
Found a new reduction rule: **.1-*.2.3.2 ==> *-.1.2

**.1.2-*.1-.2 ==> .1
-*.1.2 ==> c(.1=F)   *-1-*.1.2
-*.1.2 ==> c(.2=F)
-*.1-.2 ==> a(.2=T)



**.1.2.2 ==> c(.1=T)
    *-.1.2

## recursive reductions are required
This formula...
	**.1.3-*-.1.3 ==> -.3
...requires recursive algorithmic reductions
	**.1.3-.3 <<.1=F[-*-.1.3] first reduction
		-.3 <<after 2nd reduction
	**.1.3-.3 <<.1=T[-*-.1.3] first reduction
		-.3 <<after 2nd reduction

?Because substituting .1=F reduces formula to -.3, we can substitute .1=.3 in antecedent...
  **.3.3-*-.1.3 << .1=.3[*.1.3]
	-*-.1.3 << 2nd reduction

	-*.1.3 <<.3=F[-*-.1.3]
	-*.1.3 <<.3=T[-*-.1.3]


 ## reduce this formula... **.1*.4.3-**.4.1-**.2-.4.3

 //* (*.1*.4.3) -*(*.4.1)(-**.2-.4.3) ==> **.3.4-*.1-.4

 **.1*.4.3-*.1-*-.2.3 << .4=T[-**.4.1-**.2-.4.3]
 