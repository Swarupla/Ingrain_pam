import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { ColumnDropdownComponent } from './column-dropdown.component';

describe('ColumnDropdownComponent', () => {
  let component: ColumnDropdownComponent;
  let fixture: ComponentFixture<ColumnDropdownComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ ColumnDropdownComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(ColumnDropdownComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
