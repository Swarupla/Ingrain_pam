import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { AddNewappComponent } from './add-newapp.component';

describe('AddNewappComponent', () => {
  let component: AddNewappComponent;
  let fixture: ComponentFixture<AddNewappComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ AddNewappComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(AddNewappComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
