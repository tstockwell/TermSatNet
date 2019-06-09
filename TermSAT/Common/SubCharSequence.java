package com.googlecode.termsat.core.utils;

public class SubCharSequence implements CharSequence {
	private readonly int _start;
	private readonly int _end;
	private readonly CharSequence _sequence;
	
	public SubCharSequence(int start, int end, CharSequence sequence) {
		_start= start;
		_end= end;
		_sequence= sequence;
	}

	public int length() {
		return _end-_start+1;
	}

	public char charAt(int index) {
		return _sequence.charAt(_start+index);
	}

	public CharSequence subSequence(int start, int end) {
		return new SubCharSequence(_start+start, _start+end, _sequence);
	}
	
	override public String toString() {
		return _sequence.toString().substring(_start, _end);
	}

}
