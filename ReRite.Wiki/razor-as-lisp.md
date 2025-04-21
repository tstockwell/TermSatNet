# Symbolic Razor - Razor as Lisp

The idea is simple, it should be possible to write a specification of something, like a form, 
and then instantiate it to get a rendering of a form suitable for the current environment.  
Maybe that means that HTML was generated or a component was created in a DOM somewhere.  

Exactly what happens when a Symbolic Razor is processed depends on the template engine that processes it.  
Like Lisp programs, Symbolic Razor templates are interpreted.  
So the result of processing a Symbolic Razor template depends how a template engine interprets them.  
Symbolic Razor is rule-driven, so it's behavior depends on the rules it's using.  

For example, consider this template...
```
 DataSource TModel=Customer Id=@(()=> Id)>
	<Select>
		<Form Model=@(()=> Context)>
			<Title/>
			<Fields All/>
		</Form>
	</Select>
</DataSource>
@code {
	public string Id { get; set; }
}
```
This template will fetch a Customer record from a database by ID and display a form for it.  

DataSource is a type that fetches records given an Id.
Form is a type that defines a form for editing/viewing a record.  

The form should have a title and it should display all fields in the Customer type.  

In one application the form produced might use a Material 3 component library, 
while another might use a Bootstrap-based component library.  
But the form specification itself need never change.  
More importantly, as long as everyone understands the symbols used in it, it can be reused.  
It can be reused everywhere.  
It can be reused without changing it or adapting it.  
  
And it makes creating something like a form *so freaking easy*!.  
Because it only requires you to know how to express what you want.  
You don't have to know how anything works, or magic classnames,  
nor any of the usual bullshit that other frameworks require you to know in order to use them.  

## IComponent interface

In the previous example *Form* is an interface that extends IComponent.  
Form is a *semantic component* that used to specify a form.








