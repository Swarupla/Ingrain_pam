import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { PstTemplatesComponent } from './pst-templates.component';

describe('PstTemplatesComponent', () => {
  let component: PstTemplatesComponent;
  let fixture: ComponentFixture<PstTemplatesComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ PstTemplatesComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(PstTemplatesComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
