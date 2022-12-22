import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { MultiSelectDropDownComponent } from './multi-select-drop-down.component';

describe('MultiSelectDropDownComponent', () => {
  let component: MultiSelectDropDownComponent;
  let fixture: ComponentFixture<MultiSelectDropDownComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ MultiSelectDropDownComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(MultiSelectDropDownComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
