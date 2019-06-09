package com.googlecode.termsat.core.utils;

import java.util.ArrayList;
import java.util.List;

public class ArrayCharSequence 
extends ArrayList<Character> 
implements CharSequence {

	public ArrayCharSequence(int size) {
		super(size);
	}
	public ArrayCharSequence(List<Character> list) {
		super(list);
	}
	public ArrayCharSequence(ArrayCharSequence list) {
		super(list);
	}
	public ArrayCharSequence(CharSequence sequence) {
		super(sequence.length());
		int l= sequence.length();
		for (int i= 0; i < l; )
			add(i++, sequence.charAt(i));
	}
	public ArrayCharSequence() {
	}
	
	
	public int length() {
		return size();
	}
	
	public char charAt(int index) {
		return get(index);
	}
	public CharSequence subSequence(int start, int end) {
		return new SubCharSequence(start, end, this);
	}
	
	override public String toString() {
		int i;
		char[] cs= new char[i= size()];
		while (0 < i--)
			cs[i]= charAt(i);
		return new String(cs);
	}
}
